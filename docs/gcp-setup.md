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

## 7. Konta usługowe

### 7.1. Włącz API
konta usługowe, uprawnienia, wystawianie tokenów, wymiana tokenów z GitHub na Google, zarządzanie uprawnieniami na poziomie projektu
```bash
gcloud services enable iam.googleapis.com iamcredentials.googleapis.com sts.googleapis.com cloudresourcemanager.googleapis.com
```


### 7.2. Utworzenie kont do uruchamiania i deploymentu
runtime - uruchamia aplikację, 
deployer - wdraża
```bash
gcloud iam service-accounts create bookly-runtime --display-name="Bookly runtime (Cloud Run)"
gcloud iam service-accounts create bookly-deployer --display-name="Bookly deployer (GitHub Actions)"
```


### 7.3. Uprawnienia deployera
a) wdrażanie
```bash
gcloud projects add-iam-policy-binding bookly-pn --member="serviceAccount:bookly-deployer@bookly-pn.iam.gserviceaccount.com" --role="roles/run.developer" --condition=None
```
Rola run.developer pozwala tworzyć i aktualizować usługi, ale nie pozwala zmieniać ich uprawnień, np. tego, kto może wywołać usługę. Dlatego w workflow nie będzie flagi --allow-unauthenticated. Dostęp publiczny jest już ustawiony i pipeline nie powinien móc go zmieniać. --condition=None zapobiega interaktywnemu pytaniu o warunki.

b) wypychanie obrazów(tylko do repozytorium bookly, nie do całego projektu):
```bash
gcloud artifacts repositories add-iam-policy-binding bookly --location=europe-west1 --member="serviceAccount:bookly-deployer@bookly-pn.iam.gserviceaccount.com" --role="roles/artifactregistry.writer"
```
c) Prawo do uruchomienia usługi jako bookly-runtime (tylko na tym jednym koncie):
```bash
gcloud iam service-accounts add-iam-policy-binding bookly-runtime@bookly-pn.iam.gserviceaccount.com --member="serviceAccount:bookly-deployer@bookly-pn.iam.gserviceaccount.com" --role="roles/iam.serviceAccountUser"
```
To najmniej oczywisty element. Wdrażając usługę, która działa jako bookly-runtime, deployer de facto przekazuje jej tożsamość innemu kontu. GCP wymaga na to osobnej zgody (uprawnienie actAs). Bez niej każdy z prawem do wdrażania mógłby uruchomić kod z uprawnieniami dowolnego konta w projekcie. Nadajemy tę zgodę tylko na bookly-runtime, a nie na wszystkie konta.


### 7.4 Przełączenie aplikacji na bookly-runtime
```bash
gcloud run services update bookly --service-account=bookly-runtime@bookly-pn.iam.gserviceaccount.com
```


## 8. Workload Identity Federation (GitHub Actions → GCP)

GitHub Actions loguje się do GCP bez kluczy JSON. Dla każdego uruchomienia workflow GitHub wystawia
podpisany token OIDC (zawiera m.in. repozytorium i gałąź). GCP wymienia go na krótkotrwały (1 h) token
konta `bookly-deployer` – ale tylko jeśli token spełnia warunki dostawcy. W sekretach repo nie ma
niczego, co dałoby się ukraść.

```
token OIDC z GitHuba → STS (sprawdza wystawcę i warunek) → token federacyjny
  → IAM (sprawdza workloadIdentityUser na deployerze) → token dostępu bookly-deployer (1 h)
```

### 8.1. Pula tożsamości

Kontener na tożsamości zewnętrzne – tutaj z GitHuba.

```bash
gcloud iam workload-identity-pools create github --location=global --display-name="GitHub Actions"
```

Nie usuwać i nie tworzyć ponownie „na próbę” – usunięta pula blokuje swoje ID przez 30 dni.

### 8.2. Dostawca OIDC

```bash
gcloud iam workload-identity-pools providers create-oidc bookly-repo --location=global --workload-identity-pool=github --display-name="pnusbaum/bookly" --issuer-uri="https://token.actions.githubusercontent.com" --attribute-mapping="google.subject=assertion.sub,attribute.repository=assertion.repository,attribute.ref=assertion.ref" --attribute-condition="assertion.repository=='pnusbaum/bookly' && assertion.ref=='refs/heads/main'"
```

- `--issuer-uri` – ufamy wyłącznie tokenom wystawionym przez GitHub Actions.
- `--attribute-mapping` – które pola tokenu GitHuba są dostępne w GCP (repozytorium, gałąź).
- `--attribute-condition` – **najważniejsze**: wpuszczamy tylko repozytorium `pnusbaum/bookly` i tylko gałąź `main`.
  Bez warunku dowolne repozytorium na GitHubie mogłoby wymienić swój token w tej puli.
  Gałęzie robocze i pull requesty nie dostaną się do GCP.

Pełna nazwa dostawcy (używana w workflow):

```
projects/610029244011/locations/global/workloadIdentityPools/github/providers/bookly-repo
```

### 8.3. Pozwolenie dla repozytorium na działanie jako bookly-deployer

```bash
gcloud iam service-accounts add-iam-policy-binding bookly-deployer@bookly-pn.iam.gserviceaccount.com --role="roles/iam.workloadIdentityUser" --member="principalSet://iam.googleapis.com/projects/610029244011/locations/global/workloadIdentityPools/github/attribute.repository/pnusbaum/bookly"
```

`principalSet://…/attribute.repository/pnusbaum/bookly` = wszystkie tożsamości z puli `github`
pochodzące z repozytorium `pnusbaum/bookly`. W ścieżce jest **numer** projektu (`610029244011`), nie jego ID.

### 8.4. Weryfikacja

```bash
gcloud iam workload-identity-pools providers describe bookly-repo --location=global --workload-identity-pool=github --format="yaml(name,state,attributeCondition,attributeMapping,oidc.issuerUri)"
gcloud iam service-accounts get-iam-policy bookly-deployer@bookly-pn.iam.gserviceaccount.com
```

Oczekiwane: `state: ACTIVE`, warunek z repozytorium i gałęzią `main`, rola `roles/iam.workloadIdentityUser` dla `principalSet://…/pnusbaum/bookly`.


## Do zapamiętania

- [x] Usługa działa na domyślnym koncie usługowym Compute Engine (rola Editor na projekcie). Przed podłączeniem bucketa (Litestream): dedykowane konto usługowe z minimalnymi uprawnieniami.
- [ ] Okres próbny: przejść na płatne konto przed końcem 90 dni (termin: 01.02.2027).