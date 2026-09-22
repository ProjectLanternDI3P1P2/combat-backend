# Define broker-independent Protocol Buffers message contracts

Asynchronous Kafka message contracts use Protocol Buffers and a common
broker-independent envelope.

Kafka is the selected broker, but the application-level envelope remains free of
Kafka client types and transport-specific implementation details.

## Considered Options

Protocol Buffers provide explicit schemas, binary serialization and additive
contract evolution for independently deployed producers and consumers. This
aligns Kafka delivery with the shared gRPC contract technology (ADR-GLOB-007).

JSON remains the format of public HTTP REST APIs; it is not the Kafka event
format.

## Consequences

Messages expose common concepts such as message ID, correlation ID, causation ID,
type, version, occurrence time, producer and payload.

Application contracts are not coupled to Kafka client types, although Kafka is
the selected delivery platform.

Changing a future delivery platform does not automatically require changing
message payload contracts.
