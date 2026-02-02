# Configurazione XR per BeatSaberAlefy

## Problema
XR Origin è stato aggiunto alla scena ma non funziona nel visore.

## Soluzione

### ✅ Correzioni Applicate
1. **Run In Background abilitato** - Necessario per evitare problemi quando il visore apre il menu di sistema
2. **Vecchia Main Camera disabilitata** - Per evitare conflitti con XR Origin

### Passo 1: Configura Oculus (Provider Attuale)
1. Apri Unity Editor
2. Vai su **Edit > Project Settings > XR Plug-in Management**
3. Nella tab **PC, Mac & Linux Standalone**, assicurati che **Oculus** sia spuntato
4. Clicca su **Oculus** (sotto la lista dei loader) per verificare le impostazioni:
   - **Stereo Rendering Mode**: Single Pass Instanced (già configurato)
   - Verifica che il tuo visore sia selezionato (Quest 2, Quest 3, ecc.)

### Passo 2: Verifica la Game View
**IMPORTANTE**: Nella Game view di Unity, assicurati che il dropdown "Left Eye" sia impostato su **"Both"** o **"Center"**. 
- "Left Eye" mostra solo la vista dell'occhio sinistro (utile per debug)
- Nel visore dovresti vedere la vista completa stereoscopica

### Passo 3: Verifica la scena
- XR Origin (VR) dovrebbe essere attivo nella Hierarchy
- La camera dentro XR Origin dovrebbe avere:
  - Componente Camera con `Target Eye: Both`
  - Componente TrackedPoseDriver (già presente)

### Passo 4: Test
1. Collega il visore al PC
2. Assicurati che **Oculus Software** sia in esecuzione
3. Se usi Quest, attiva **Oculus Link** o **Air Link**
4. Premi Play in Unity Editor
5. **Nel visore** dovresti vedere la scena completa, non solo l'occhio sinistro

## Risoluzione Problemi

### Vedo solo l'occhio sinistro nel visore
- Verifica che nella Game view di Unity non sia selezionato "Left Eye"
- Controlla che la camera VR abbia `Target Eye: Both` nell'Inspector
- Riavvia Unity Editor dopo le modifiche

### Non vedo nulla nel visore
- Verifica che Oculus Software sia in esecuzione
- Controlla che Oculus Link/Air Link sia attivo (per Quest)
- Verifica che XR Origin sia attivo nella scena
- Controlla la Console di Unity per errori

### Oculus Dash si apre automaticamente / l'app si blocca
- **Problema**: Oculus Dash (menu con lampadina e joystick) si apre automaticamente dopo 1 secondo
- **Causa**: L'applicazione perde il focus o c'è un errore che causa il freeze
- **Soluzione**:
  1. Verifica che **VRFocusManager** sia presente nella scena (viene aggiunto automaticamente con XR Origin)
  2. Controlla la Console di Unity per errori che potrebbero causare il crash
  3. Assicurati che la scena Game abbia una traccia selezionata (altrimenti carica il Menu)
  4. Verifica che **Run In Background** sia abilitato in Project Settings > Player
  5. Se il problema persiste, prova a disabilitare **Dash Support** in Project Settings > XR Plug-in Management > Oculus

### Le spade non si muovono / vedo lampadine invece delle spade
- **Problema**: Vedi due "lampadine" (controller XR) che non si muovono invece delle spade
- **Causa**: I Saber non sono collegati correttamente ai controller XR reali
- **Soluzione**:
  1. Verifica che **VRControllerSetup** sia presente nella scena (viene aggiunto automaticamente con XR Origin)
  2. Assicurati che il prefab **Saber** esista in `Assets/VR/Saber.prefab`
  3. Se i controller non vengono trovati automaticamente:
     - Vai su **BeatSaberAlefy > Setup > Add Saber to XR Controllers**
     - Oppure collega manualmente i Saber ai controller XR nella Hierarchy
  4. Verifica che XR Interaction Toolkit sia installato e configurato correttamente

### L'ambiente ruota quando giro la testa
- **Problema**: Quando giri la testa, tutta la stanza/ambiente ruota con te
- **Causa**: Tracking origin mode non configurato correttamente
- **Soluzione**:
  1. Verifica che XR Origin abbia **Requested Tracking Origin Mode** = **Floor** (già configurato automaticamente)
  2. Se il problema persiste, verifica le impostazioni Oculus:
     - **Project Settings > XR Plug-in Management > Oculus**
     - **Enable Tracking Origin Stage Mode** dovrebbe essere disabilitato per ambiente fisso
  3. Riavvia Unity Editor dopo le modifiche

### Menu non accessibile in VR
- **Problema**: Il menu non è visibile o accessibile nel visore
- **Causa**: Il menu è configurato per desktop (Screen Space Overlay)
- **Soluzione**:
  1. Verifica che **VRMenu** sia presente nella scena (viene aggiunto automaticamente con XR Origin)
  2. Il menu VR viene creato automaticamente come Canvas world-space davanti al player
  3. Se il menu non appare:
     - Verifica che **VRMenu** sia attivo nella Hierarchy
     - Controlla la distanza del menu (default: 2 metri davanti al player)
     - Verifica che la camera VR sia configurata correttamente

### OpenXR vs Oculus
- **Oculus**: Usa questo se hai un visore Oculus (consigliato)
- **OpenXR**: Richiede configurazione aggiuntiva (interaction profiles)
- Se usi Oculus, mantieni Oculus abilitato e OpenXR disabilitato

## Note
- Il "Left Eye" nella Game view è solo per la preview nell'editor, non influisce sul visore
- Se vedi qualcosa dall'occhio sinistro ma non il gioco completo, potrebbe essere un problema di rendering stereoscopico - verifica le impostazioni Oculus
