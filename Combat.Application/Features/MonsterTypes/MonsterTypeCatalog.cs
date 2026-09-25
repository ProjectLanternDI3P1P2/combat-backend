using Combat.Application.Exceptions;
using Combat.Application.Models;
using Combat.Application.Ports;
using FluentValidation;
using FluentValidation.Results;

namespace Combat.Application.Features.MonsterTypes;

public sealed class MonsterTypeCatalog(
    IMonsterTypeSource source,
    IValidator<MonsterTypeDefinition> validator) : IMonsterTypeCatalog
{
    public async Task<IReadOnlyList<MonsterTypeDefinition>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<MonsterTypeDefinition>? sourceDefinitions =
            await source.GetAllAsync(cancellationToken);

        if (sourceDefinitions is null)
        {
            throw new InvalidMonsterTypeCatalogException(new Dictionary<string, string[]>
            {
                ["monsterTypes"] = ["The source returned no catalog."]
            });
        }

        MonsterTypeDefinition[] definitions = [.. sourceDefinitions];
        Dictionary<string, List<string>> errors = [];

        if (definitions.Length == 0)
        {
            AddError(errors, "monsterTypes", "The catalog must contain at least one monster type.");
        }

        for (int index = 0; index < definitions.Length; index++)
        {
            MonsterTypeDefinition? definition = definitions[index];
            if (definition is null)
            {
                AddError(errors, $"monsterTypes[{index}]", "Monster type definition is required.");
                continue;
            }

            ValidationResult result = await validator.ValidateAsync(definition, cancellationToken);

            foreach (ValidationFailure failure in result.Errors)
            {
                AddError(errors, $"monsterTypes[{index}].{failure.PropertyName}", failure.ErrorMessage);
            }
        }

        foreach (IGrouping<Guid, MonsterTypeDefinition> duplicate in definitions
                     .OfType<MonsterTypeDefinition>()
                     .GroupBy(monsterType => monsterType.Id)
                     .Where(group => group.Count() > 1))
        {
            AddError(
                errors,
                "monsterTypes",
                $"Monster type Id '{duplicate.Key}' is duplicated.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidMonsterTypeCatalogException(
                errors.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray()));
        }

        return Array.AsReadOnly(definitions);
    }

    public async Task<IReadOnlyList<MonsterTypeDefinition>> ResolveRequiredAsync(
        IReadOnlyCollection<Guid> monsterTypeIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(monsterTypeIds);

        List<ValidationFailure> failures = [];
        if (monsterTypeIds.Any(monsterTypeId => monsterTypeId == Guid.Empty))
        {
            failures.Add(new ValidationFailure("MonsterTypeIds", "Monster type IDs must not be empty."));
        }

        Guid[] duplicateIds = [.. monsterTypeIds
            .GroupBy(monsterTypeId => monsterTypeId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)];

        if (duplicateIds.Length > 0)
        {
            failures.Add(new ValidationFailure(
                "MonsterTypeIds",
                $"Monster type IDs must be unique. Duplicates: {string.Join(", ", duplicateIds)}."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        IReadOnlyList<MonsterTypeDefinition> catalog = await GetAllAsync(cancellationToken);
        IReadOnlyDictionary<Guid, MonsterTypeDefinition> definitionsById =
            catalog.ToDictionary(monsterType => monsterType.Id);

        Guid[] unknownIds = [.. monsterTypeIds
            .Where(monsterTypeId => !definitionsById.ContainsKey(monsterTypeId))];

        if (unknownIds.Length > 0)
        {
            throw new KeyNotFoundException(
                $"Unknown monster type IDs: {string.Join(", ", unknownIds)}.");
        }

        MonsterTypeDefinition[] resolved = [.. monsterTypeIds.Select(monsterTypeId => definitionsById[monsterTypeId])];
        return Array.AsReadOnly(resolved);
    }

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string key,
        string message)
    {
        if (!errors.TryGetValue(key, out List<string>? messages))
        {
            messages = [];
            errors[key] = messages;
        }

        messages.Add(message);
    }
}
