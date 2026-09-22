# Communication patterns for the game platform

The platform uses Traefik as its only public entry point, SignalR/WebSocket for gameplay, HTTPS/REST for non-gameplay frontend operations, gRPC with Protocol Buffers for immediate service-to-service calls, and RabbitMQ with versioned Protocol Buffers contracts for idempotent asynchronous business events. This preserves service ownership while keeping the MVP small: no Redis backplane is enabled until Dungeon or Combat must scale beyond one replica.
