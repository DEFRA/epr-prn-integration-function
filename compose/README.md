# Running all services via docker compose

## Function dependencies

See `epr-calculator-frontend/compose` and copy the .env.example file, obtaining the secret(s) needed from a fellow dev.

From the `epr-prn-integration-function` folder in a terminal, run:

```
docker compose up asb azurite http-api-mocks -d
```

Services that will be started:

**Azure service bus emulator**

Configures the queues, see `compose/asb.json`.

**Azurite**

Used for blob storage under port 10000.

See `azurite-init` service for initial container configuration needed by dependent services.

To stop all services run:

```
docker compose down
```

The SQL Edge and Azurite services use persisted volumes so data will be retained following a restart. Be mindful that
these may need clearing down if using the same calculation run names, as generated files within blob storage may start
to clash.

To remove volumes:

```
docker compose down -v
```

Logs for each service can be viewed via:

```
docker compose logs <service name>
```

## Running the function

### With a debugger in an IDE

`local.settings.json` contains config that will point to the dockerised services above. Copy the .example file:

`cp src/EprPrnIntegration.Api/local.settings.json.example cp src/EprPrnIntegration.Api/local.settings.json`

**You'll need to update your own copy with a gov.notify API (TEST) key for the `MessagingConfig:ApiKey` env but check
the .json file for details.**

Once you're configured you can spin up the function via your IDE, to attach and set breakpoints

Then, invoke the chosen function e.g.  `FetchNpwdIssuedPrnsFunction`:

`./scripts/trigger-local-function.sh FetchNpwdIssuedPrnsFunction`

The functions are void-style so it's recommended to tail your logs/watch IDE output if you want to track behaviour.

### With the function hosted within Docker

`docker compose up --build -d`

Then issue commands to the container via another helper script:

`./scripts/trigger-container-function.sh FetchNpwdIssuedPrnsFunction`
