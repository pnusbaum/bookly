# Konfiguracja GCP – Bookly

| | |
|---|---|
| Project ID | `bookly-pn` |
| Project number | `610029244011` |
| Region | `europe-west1` |
| Usługa | https://bookly-610029244011.europe-west1.run.app |
| GCP | https://console.cloud.google.com/home/dashboard?project=bookly-pn |

Wszystkie polecenia uruchamiane z głównego folderu repo (PowerShell).

## 1. Logowanie i projekt

```bash
gcloud auth login
gcloud projects create bookly-pn --name="Bookly"
gcloud config set project bookly-pn
```

Project ID jest globalnie unikalny i niezmienny. `--name` to tylko nazwa wyświetlana.
`projects create` nie ustawia projektu jako aktywnego, stąd `config set project`.

## 2. Rozliczenia

Konto rozliczeniowe utworzone w konsoli: *Billing → Create account*, typ *Individual*.

```bash
gcloud billing accounts list
gcloud billing projects link bookly-pn --billing-account=<BILLING_ACCOUNT_ID>
```

## 3. Budżety

*Billing → Cost control → Budgets & caps*

| Budżet | Typ | Zakres | Kwota | Działanie |
|---|---|---|---|---|
| `bookly-budget` | tylko alerty | bookly-pn, wszystkie usługi | 5 PLN / mies. | maile przy 50%, 90%, 100% |
| `bookly-spend-cap` | spend cap | bookly-pn, tylko Cloud Run | 10 PLN / mies. | po przekroczeniu Cloud Run wstrzymany do końca miesiąca lub ręcznego zdjęcia limitu |

- Budżetu z alertami nie da się przekształcić w budżet z limitem, trzeba utworzyć nowy.
- Spend cap liczy koszt brutto (bez kredytów) i działa z opóźnieniem kilku godzin.
- Spend cap nie obejmuje Artifact Registry ani Cloud Storage, te pilnuje tylko `bookly-budget`.

Odczyt budżetów z gcloud wymaga Budget API:

```bash
gcloud services enable billingbudgets.googleapis.com
gcloud billing budgets list --billing-account=<BILLING_ACCOUNT_ID>
```

## 4. Domyślny region i API

```bash
gcloud config set run/region europe-west1
gcloud config set artifacts/location europe-west1
gcloud services enable run.googleapis.com artifactregistry.googleapis.com
```

## 5. Artifact Registry

```bash
gcloud artifacts repositories create bookly --repository-format=docker --location=europe-west1 --description="Bookly container images"
```

Polityka czyszczenia: [`infra/artifact-registry-cleanup.json`](../infra/artifact-registry-cleanup.json).
3 najnowsze obrazy zostają zawsze, starsze niż 7 dni są usuwane (reguła Keep ma pierwszeństwo przed Delete).

```bash
# najpierw test, potem właściwe zastosowanie
gcloud artifacts repositories set-cleanup-policies bookly --location=europe-west1 --policy=infra/artifact-registry-cleanup.json --dry-run
gcloud artifacts repositories set-cleanup-policies bookly --location=europe-west1 --policy=infra/artifact-registry-cleanup.json --no-dry-run
```

Logowanie Dockera do rejestru (gcloud jako credential helper):

```bash
gcloud auth configure-docker europe-west1-docker.pkg.dev
```

## 6. Ręczne wdrożenie

```bash
docker build -t europe-west1-docker.pkg.dev/bookly-pn/bookly/bookly:v1 .
docker push europe-west1-docker.pkg.dev/bookly-pn/bookly/bookly:v1
gcloud run deploy bookly --image=europe-west1-docker.pkg.dev/bookly-pn/bookly/bookly:v1 --allow-unauthenticated --min-instances=0 --max-instances=1 --memory=512Mi --cpu=1
gcloud run services update bookly --max=1
```

- `--max-instances` – limit dla rewizji, zapisany w niej na stałe.
- `--max` – limit dla całej usługi, zmieniany bez nowego wdrożenia.
- `max = 1` – twardy limit kosztów i wymóg SQLite (jeden zapisujący).

Weryfikacja:

```bash
gcloud run services describe bookly --format="yaml(metadata.annotations, spec.template.metadata.annotations, spec.template.spec.containers[0].resources)"
```

## Do zapamiętania

- [ ] Usługa działa na domyślnym koncie usługowym Compute Engine (rola Editor na projekcie). Przed podłączeniem bucketa (Litestream): dedykowane konto usługowe z minimalnymi uprawnieniami.
- [ ] Okres próbny: przejść na płatne konto przed końcem 90 dni (termin: 01.02.2027).