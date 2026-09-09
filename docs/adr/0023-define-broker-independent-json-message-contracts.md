# Define broker-independent JSON message contracts

Asynchronous message contracts use JSON and a common broker-independent envelope.

The contract convention is defined before the message broker is selected.

## Considered Options

The broker requires a separate evaluation involving expected data volume,
operational constraints and delivery behavior.

Those questions do not prevent the application teams from standardizing message
contracts. Most candidate brokers can transport JSON payloads with common
metadata.

JSON is readable, easy to inspect and debug, broadly interoperable and sufficient
for the expected application contracts without requiring a schema-specific binary
format such as Protobuf or Avro.

## Consequences

Messages expose common concepts such as message ID, correlation ID, causation ID,
type, version, occurrence time, producer and payload.

Application contracts are not designed around RabbitMQ, Kafka or another broker
before that infrastructure decision is made.

Changing the future broker does not automatically require changing message
payload contracts.
