# BeatSaberAlefy – Quick start

## Apertura in Unity

1. Apri **Unity Hub** e aggiungi il progetto: **Add** → **Add project from disk** → seleziona la cartella **BeatSaberAlefy** (non la root `meta`).
2. Usa **Unity 6 LTS** (es. 6000.3.x) o **Unity 2022 LTS**. Se i pacchetti XR mancano, da **Window → Package Manager** aggiungi **Oculus XR Plugin** e **XR Interaction Toolkit**.

## Setup iniziale (prefab e scene)

1. In Unity: menu **BeatSaberAlefy → Setup → Create All (Prefabs + Scenes)**.
2. Vengono creati:
   - **Assets/Cubes/Cube.prefab** – cubo con Sliceable e CubeMover
   - **Assets/VR/Saber.prefab** – spada con SliceDetector
   - **Assets/Scenes/Menu.unity** – menu (Seleziona canzone, Analisi, Avvia partita)
   - **Assets/Scenes/Game.unity** – scena di gioco con GameplayDirector e SpawnController
3. **BeatSaberAlefy → Setup → Add Scenes to Build Settings** per includere Menu e Game nel build (la **prima** scena deve essere **Menu** così il gioco parte dal menu).
4. Se i pulsanti del menu non rispondono: **BeatSaberAlefy → Setup → Add EventSystem to scene** (usa Input System; se la scena ha già un EventSystem con modulo vecchio, viene sostituito).

## Test senza VR

1. Apri la scena **Menu**. Se l’hai creata con **Create Menu Scene**, i riferimenti a **MenuController** (Carica da Alefy, Cerca, Avvia, StatusText, lista tracce) sono già collegati; altrimenti collegali in Inspector.
2. Metti un file audio in **Assets/Resources/Audio/** e rinominalo in **TestClip** (o crea un AudioClip e assegnarlo da codice/Resources).
3. Play: Seleziona canzone → Analizza → Avvia partita. Si carica la scena Game con audio e cubi (senza XR Origin i cubi spawnano davanti alla camera).

## Test con Oculus Rift S

1. Installa **Oculus App** e collega il Rift S.
2. Nella scena **Game**: aggiungi **XR Origin** (da XR Interaction Toolkit) e posiziona le due **Saber** come figli dei controller sinistro e destro.
3. Su ogni Saber: imposta **SliceDetector.AudioTimeProvider** sul GameObject che ha **GameplayDirector** e **AudioTimeMethodName** = `GetAudioTime`. Imposta **SaberLane** (0 = sinistra, 1 = destra).
4. Su **SpawnController**: assegna **Cube Prefab** = `Assets/Cubes/Cube.prefab`, **Player Forward** = XR Origin Camera o riga centrale del visore.

## API Alefy

- **IAlefyClient** e **AlefyService** sono in `Assets/Alefy/`. API: GET /api/tracks, GET /api/tracks/:id, GET /api/stream/tracks/:id (vedi `EXTERNAL_API.md` nella root del repo).
- **Configurazione**: menu **BeatSaberAlefy → Setup → Create Alefy Settings**. Si crea `Assets/Resources/Settings/AlefySettings.asset`. Apri l'asset, incolla il **token API** in **AuthToken** e salva. L'asset è in `.gitignore` (non committare il token).
- **Menu**: pulsante **Carica da Alefy** carica l'elenco tracce; scegli una dalla lista (o viene caricata la prima). Download e analisi ritmo come per il file locale.; per usare alefy, in **MenuController** sostituisci **OnSelectSong** con una chiamata a `_alefyClient.GetSongsAsync()` e **PrepareSongAsync** per il download dell’audio.
