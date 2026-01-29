# VR Beat Saber – Canzoni da alefy.alevale.it

Gioco VR in stile Beat Saber per **Oculus Rift S**: ambiente sci-fi, cubi che arrivano a tempo di musica, due spade laser controllate dai controller. Le canzoni vengono da [alefy.alevale.it](https://alefy.alevale.it); il sistema analizza il ritmo (BPM, beat) e quando tutto è pronto parte il gioco.

---

## Indice

- [Panoramica](#panoramica)
- [Documentazione di progetto](#documentazione-di-progetto)
- [Requisiti](#requisiti)
- [Installazione](#installazione)
- [Struttura del repository](#struttura-del-repository)
- [Apertura del progetto](#apertura-del-progetto)
- [Esecuzione e test](#esecuzione-e-test)
- [Sviluppo](#sviluppo)
- [Risoluzione problemi](#risoluzione-problemi)

---

## Panoramica

- **Piattaforma**: Windows, Oculus Rift S.
- **Engine**: Unity 6 LTS o Unity 2022 LTS.
- **Flusso**: scelta canzone da alefy → download/audio → analisi ritmo (BPM, offset, beat grid) → “Tutto pronto” → avvio partita → cubi a tempo, slice con spade laser.
- **Stile**: ambiente fantascientifico, neon, cubi colorati, spade con trail.

Per architettura, deliverable e ordine di implementazione vedi **[PLAN.md](PLAN.md)**.

---

## Documentazione di progetto

| File | Contenuto |
|------|-----------|
| **[PLAN.md](PLAN.md)** | Piano completo: stack, ambiente di sviluppo, programmi da installare, risorse hardware, OS, architettura, integrazione alefy, analisi ritmo, beat map, VR, stile, ordine di implementazione. |
| **README.md** (questo file) | Istruzioni per clonare, installare, aprire e far girare il progetto. |

---

## Requisiti

### Sistema operativo

- **Windows 10 o 11 (64-bit)**. Rift S e Oculus sono supportati ufficialmente solo su Windows.

### Hardware (sintesi)

- **Minimo**: CPU i5/Ryzen 5, 16 GB RAM, GPU GTX 1060 6 GB / RX 580, SSD 256 GB, DisplayPort 1.2 + USB 3.0.
- **Consigliato**: i7/Ryzen 7, 32 GB RAM, RTX 3060 / RX 6600, SSD 512 GB–1 TB.

Dettaglio in [PLAN.md – Risorse hardware](PLAN.md#risorse-hardware-per-dimensionare-la-macchina).

### Software da installare

| Software | Uso |
|----------|-----|
| **Unity Hub** | Gestione installazioni Unity e progetti. |
| **Unity 6 LTS** o **Unity 2022 LTS** | Engine di gioco. Da Hub: aggiungi modulo **Windows Build Support**. |
| **Cursor** (o VS Code / Visual Studio) | IDE per C#. In Cursor: estensione **C#** (Microsoft). |
| **Oculus App** | [Download](https://www.meta.com/quest/setup) – driver e runtime per Rift S. |
| **Git** | Clonazione e versionamento. |

Dettaglio in [PLAN.md – Programmi e dipendenze](PLAN.md#programmi-e-dipendenze-da-installare).

---

## Installazione

### 1. Clonare il repository

```powershell
git clone https://github.com/TUO_USERNAME/meta.git
cd meta
```

Sostituisci `TUO_USERNAME` con il tuo username GitHub. Se usi SSH:

```powershell
git clone git@github.com:TUO_USERNAME/meta.git
cd meta
```

### 2. Creare il progetto Unity (prima volta)

Se la cartella `BeatSaberAlefy` non esiste ancora:

1. Apri **Unity Hub**.
2. **Add** → **New project**.
3. Scegli template **3D (Core)**.
4. Nome progetto: `BeatSaberAlefy`.
5. **Location**: `c:\Users\Valerio\meta` (o il path dove hai clonato `meta`). Unity creerà `meta\BeatSaberAlefy\`.
6. **Create project**.

Se il progetto esiste già, vai al passo 3.

### 3. Aggiungere i pacchetti Unity (XR, Oculus)

1. Con il progetto aperto in Unity: **Window** → **Package Manager**.
2. **+** → **Add package by name** (o **Add package from git URL** se previsto).
3. Aggiungi:
   - **Oculus XR Plugin**: nome pacchetto `com.unity.xr.oculus`.
   - **XR Interaction Toolkit**: `com.unity.xr.interaction.toolkit`.
   - Se richiesto da Oculus: **OpenXR Plugin** (`com.unity.xr.openxr`).
4. **Install** / **Add** e attendi la risoluzione delle dipendenze.

Riferimenti: [Oculus XR Plugin](https://docs.unity3d.com/Packages/com.unity.xr.oculus@latest), [XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest).

### 4. Impostare Cursor come editor esterno (opzionale)

1. In Unity: **Edit** → **Preferences** (Windows) o **Unity** → **Preferences** (macOS).
2. **External Tools**.
3. **External Script Editor**: seleziona **Cursor** (se presente) oppure **Browse** e indica l’eseguibile di Cursor.
4. Opzionale: spunta **External Script Editor Args** se ti serve passare argomenti.

Dopo questo, il doppio clic su uno script in Unity aprirà Cursor.

### 5. Oculus Rift S

1. Installa **Oculus App** dal link indicato sopra.
2. Collega il Rift S (DisplayPort + USB 3.0).
3. Completa la configurazione guidata e verifica che il visore funzioni dal desktop.
4. In Unity, per testare in VR: avvia il gioco con **Play** e indossa il visore (il build target deve essere **PC** e XR abilitato nella scena).

---

## Struttura del repository

```
meta/
├── README.md           ← istruzioni (questo file)
├── PLAN.md             ← piano di progetto completo
├── .gitignore          ← ignora Library, Temp, build, secret, ecc.
└── BeatSaberAlefy/     ← progetto Unity (apri questa cartella in Unity)
    ├── Assets/
    │   ├── Alefy/      → client API alefy (da implementare)
    │   ├── Audio/      → RhythmAnalyzer, RhythmData
    │   ├── BeatMap/    → BeatMapGenerator, SpawnController
    │   ├── VR/         → XR Origin, Saber, input
    │   ├── Environment/
    │   ├── Cubes/
    │   ├── UI/
    │   └── Settings/
    ├── ProjectSettings/
    ├── Packages/
    └── ...
```

Dettaglio in [PLAN.md – Struttura progetto Unity](PLAN.md#struttura-progetto-unity-suggerita).

---

## Apertura del progetto

### In Unity

1. Apri **Unity Hub**.
2. **Add** → **Add project from disk**.
3. Seleziona la cartella **`BeatSaberAlefy`** (non la root `meta`).
4. Doppio clic sul progetto per aprirlo.

### In Cursor (o VS Code)

1. Apri Cursor.
2. **File** → **Open Folder**.
3. Puoi aprire:
   - **`meta`** – intero repo (README, PLAN, BeatSaberAlefy).
   - **`meta/BeatSaberAlefy`** – solo progetto Unity (utile se Cursor usa i file `.csproj`/`.sln` generati da Unity).

I file `.csproj` e `.sln` sono generati da Unity alla prima apertura del progetto; se non li vedi, apri prima il progetto in Unity.

---

## Esecuzione e test

### Play in Editor

1. Apri il progetto **BeatSaberAlefy** in Unity.
2. Apri la scena di gioco (es. `Assets/.../Game.unity` quando sarà creata).
3. Premi **Play**.
4. Per test VR: indossa il Rift S; assicurati che XR sia abilitato e che la scena contenga XR Origin e i controller.

### Build eseguibile

1. **File** → **Build Settings**.
2. **Platform**: Windows.
3. **Add Open Scenes** per includere le scene necessarie.
4. **Build** o **Build And Run** e scegli la cartella di output.

---

## Sviluppo

- **Branch**: lavora su un branch (es. `develop` o `feature/nome`) e fai merge su `main` dopo review o test.
- **Commit**: messaggi chiari; evita di committare `Library/`, `Temp/`, file di configurazione locale con segreti (il `.gitignore` li esclude).
- **Credenziali alefy**: non mettere API key o token nel codice; usa variabili d’ambiente, ScriptableObject non versionato, o un backend proxy. Vedi [PLAN.md – Dipendenze da te](PLAN.md#dipendenze-da-te).

Ordine di implementazione suggerito: [PLAN.md – Ordine di implementazione](PLAN.md#ordine-di-implementazione-suggerito).

---

## Risoluzione problemi

| Problema | Cosa verificare |
|----------|------------------|
| Unity non vede il Rift S | Oculus App installata e visore collegato; in Build Settings target **PC**; nella scena XR abilitato (Oculus XR Plugin). |
| Cursor non si apre da Unity | **Edit** → **Preferences** → **External Tools** → **External Script Editor** impostato su Cursor; percorso eseguibile corretto. |
| Pacchetti XR in errore | **Window** → **Package Manager** → aggiorna pacchetti; controlla che la versione di Unity sia 2022 LTS compatibile con Oculus XR Plugin. |
| Build fallisce | **File** → **Build Settings** → scene aggiunte; nessun errore in **Console**; spazio disco sufficiente. |

Per requisiti ufficiali e link: [PLAN.md – Riferimenti](PLAN.md#risorse-hardware-per-dimensionare-la-macchina).
