# Running all services via docker compose

This assumes all services are cloned as follows:

```
root
|- epr-calculator-api
|- epr-calculator-frontend
|- epr-calculator-service
|- epr-calculator-fss-api
```

See `epr-calculator-frontend/compose` and copy the .env.example file, obtaining the secret(s) needed from a fellow dev.

From the `epr-calculator-service` folder in a terminal, run:

```
docker compose up --build -d
```

Services that will be started:

**Azure service bus emulator**

Configures a single queue, see `compose/asb.json`.

**SQL edge**

Runs on standard 1433 port, see `compose.yml` for username/password so you can connect locally.

See `migrations` service for how PayCal database is created, migrations are run and then initial seeding.

The seed file sets up some dev organisation and POM data so the Synapse pipeline is not required.

**Azurite**

Used for blob storage under port 10000.

See `azurite-init` service for initial container configuration needed by dependent services.


Visit `https://localhost:7163/`

You may need to clear cookies if you've logged in previously and are receiving an error.

Sign in with your @Defra.onmicrosoft.com account.

Configure the default parameters and local authority disposal costs by uploading the relevant CSV files. The CSV files can be obtained from a fellow dev.

Visit `https://localhost:7163/ViewDefaultParameters` and upload the defaultParamsAsPerDemonstrator.csv

Visit `https://localhost:7163/ViewLocalAuthorityDisposalCosts` and upload the laCostsAsPerDemonstrator.csv

Visits `https://localhost:7163/RunANewCalculation` and complete a calculation run through to billing file creation.

To stop all services run:

```
docker compose down
```

The SQL Edge and Azurite services use persisted volumes so data will be retained following a restart. Be mindful that these may need clearing down if using the same calculation run names, as generated files within blob storage may start to clash.

To remove volumes:

```
docker compose down -v
```

Logs for each service can be viewed via:

```
docker compose logs epr-service
docker compose logs epr-frontend
docker compose logs epr-api
docker compose logs epr-fss-api
```
