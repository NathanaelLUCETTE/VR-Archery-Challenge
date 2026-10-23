# VR Archery Challenge

Jeu de tir à l’arc en réalité virtuelle réalisé avec Unity pour le Meta Quest 2. Le joueur prend une flèche dans un carquois, l’encoche sur son arc, tend la corde et vise des cibles pour marquer des points. Le projet propose une progression sur quatre niveaux, des équipements à débloquer et un classement local.

## Démarrage rapide

1. Installer **Unity Hub** et **Unity 6000.6.0f1**, version indiquée dans `ProjectSettings/ProjectVersion.txt`.
2. Récupérer le projet complet, notamment les dossiers `Assets`, `Packages` et `ProjectSettings`, avec les fichiers `.meta`.
3. Dans Unity Hub, ajouter le dossier du projet puis l’ouvrir avec cette version de Unity.
4. Attendre la fin de l’importation des ressources et la résolution des packages. Une connexion Internet et Git sont nécessaires pour récupérer les dépendances, dont le package Unity MCP référencé par une URL Git.
5. Ouvrir **`Assets/Scenes/SampleScene.unity`**, la scène principale et la scène activée dans les paramètres de build.
6. Lancer **Play**, puis choisir le mode Entraînement pour découvrir le tir.

Le premier chargement peut être long. Les dossiers `Library`, `Temp` et `Logs` sont générés par Unity et ne sont pas nécessaires au transfert du projet.

### Avec un Meta Quest 2

Pour tester depuis l’éditeur sur PC, connecter le casque via Quest Link ou Air Link et établir la connexion VR avant de lancer Play. Le suivi du casque, les deux contrôleurs et les interactions doivent être vérifiés dans la scène.

Pour préparer une version autonome sur le casque, installer les modules **Android Build Support**, **Android SDK & NDK Tools** et **OpenJDK** de la même version de Unity. Sélectionner Android dans les profils de build, vérifier la configuration OpenXR pour cette plateforme et conserver `SampleScene` dans les scènes à construire. Connecter le casque en mode développeur, autoriser le débogage USB, puis utiliser Build And Run. Cette procédure nécessite une validation sur le casque après compilation.

### Sans casque

Le projet contient le **XR Interaction Simulator** pour les essais au clavier et à la souris. Utiliser son interface d’aide pour consulter les touches associées aux mains et aux actions. La simulation sert à contrôler le fonctionnement ; elle ne remplace pas un test des gestes au casque.

## Comment jouer

1. Choisir un niveau et un équipement accessibles dans le menu **Niveau/équipement**.
2. Lancer une partie ou le mode Entraînement.
3. Attraper le corps de l’arc avec le grip d’une main.
4. Avec l’autre main, saisir une flèche dans le carquois.
5. Amener la flèche près de l’encoche et de la corde : elle se place automatiquement lorsqu’elle entre dans la zone de chargement.
6. Garder le grip et reculer la main pour tendre la corde.
7. Viser, puis relâcher le grip de la main qui tend la corde pour tirer.

Le carquois renouvelle les flèches disponibles. Pour les menus VR, pointer un bouton avec le rayon du contrôleur puis utiliser l’action de clic UI, généralement la gâchette avec les actions du rig.

## Modes de jeu

### Progression classique

| Niveau | Comportement des cibles |
| --- | --- |
| 1 | Cibles fixes |
| 2 | Cibles en mouvement |
| 3 | Cibles qui apparaissent et disparaissent |
| 4 | Cibles en mouvement qui apparaissent et disparaissent |

Les difficultés **Facile**, **Normal** et **Difficile** règlent la durée, la taille et la vitesse des cibles. En Difficile, les cibles dérivent également sur les niveaux habituellement fixes. Les objectifs sont ajustés selon la durée de la difficulté choisie ; les valeurs affichées en jeu font référence.

Atteindre l’objectif à la fin de la partie débloque le niveau suivant et de nouveaux équipements. L’entraînement permet de tirer sans limite de temps, sans débloquer la progression.

### Modes supplémentaires

| Mode | Principe |
| --- | --- |
| Hardcore | Toucher des cibles volantes ; objectif de 70 points en 60 secondes |
| Extrême | Abattre des aigles ; objectif de 50 points en 90 secondes |
| Dragon | Affronter un dragon en surveillant sa vie ; limite de 180 secondes |

Le mode Dragon peut se terminer avant la fin du chronomètre si le dragon est vaincu ou si le joueur perd tous ses points de vie.

## Score et équipement

Les cibles classiques possèdent **10 zones concentriques** : le centre rapporte **10 points**, puis les zones rapportent 9, 8, 7, 6, 5, 4, 3, 2 et 1 point vers l’extérieur. Un impact hors du disque rapporte 0 point. Les impacts dans le menu ou après la fin de la partie ne comptent pas.

| Palier | Arc | Multiplicateur de l’arc | Flèche | Multiplicateur de la flèche |
| --- | --- | --- | --- | --- |
| Initial | Bois | ×1 | Standard | ×1 |
| Après le niveau 1 | Chasseur | ×1,25 | Légère | ×1,15 |
| Après le niveau 2 | Carbone | ×1,5 | Carbone | ×1,3 |
| Après le niveau 3 | Élite | ×2 | Élite | ×1,75 |

Les deux multiplicateurs se combinent pour le tir. Ils modifient la propulsion de la flèche, pas directement le nombre de points par zone.

## Menus et sauvegardes

- **Classement** : consultation des scores enregistrés localement.
- **Enregistrer mon score** : saisie du nom et enregistrement depuis les résultats.
- **Rejouer / Retour au menu** : navigation après une partie.
- **Volume** : réglage accessible dans le menu et pendant la partie.
- **Réinitialiser** : effacement des données locales du jeu après confirmation.

Le classement est stocké dans `scores.json`, dans le dossier désigné par `Application.persistentDataPath`. La progression et le volume sont conservés avec `PlayerPrefs`. Le classement est local : il n’est pas synchronisé entre les ordinateurs ou les casques.

## Organisation du projet

| Emplacement | Rôle |
| --- | --- |
| `Assets/Scenes/SampleScene.unity` | Scène principale |
| `Assets/Scenes/Test.unity` | Scène de test conservée |
| `Assets/Scripts` | Scripts de jeu, interactions, progression et interface |
| `Assets/Prefabs` | Objets réutilisables, dont l’arc et les flèches |
| `Assets/Resources` | Ressources chargées à l’exécution : audio, équipements et modes spéciaux |
| `Packages/manifest.json` | Dépendances Unity |
| `ProjectSettings` | Configuration du projet |
| `Tests` | Vérifications de la sauvegarde des scores et de la progression |

Principaux scripts :

- `GameSession` : états du jeu, chronomètre, objectifs et résultats.
- `QuiverSpawner`, `ManualArrow`, `ArrowSpawner` : génération, saisie et chargement des flèches.
- `PullInteraction`, `Arrow` : tension de la corde, lancement et impacts.
- `TargetScore`, `MovingTarget`, `TargetVisibility` : score et comportement des cibles.
- `ProgressionRules`, `ProgressionManager` : déblocages, équipement et sauvegarde.
- `LeaderboardUI`, `ScoreRepository` : interface du classement et stockage des scores.
- `SpecialModes` : modes Hardcore, Extrême et Dragon.

Dépendances principales déclarées : **XR Interaction Toolkit 3.6.1**, **OpenXR 1.18.0**, **Input System 1.20.0** et **Universal Render Pipeline 17.6.0**. Le manifeste constitue la référence complète.

## Vérifications avant une démonstration

- Ouvrir `SampleScene` et vérifier l’absence d’erreurs de compilation dans la Console.
- Tester la saisie de l’arc, la récupération d’une flèche, l’encochage et le tir.
- Vérifier qu’un impact au centre rapporte davantage qu’un impact en périphérie.
- Tester les quatre comportements de cibles et les modes supplémentaires.
- Vérifier les boutons à distance, le réglage du volume et le retour au menu.
- Terminer une partie, enregistrer un score et vérifier sa présence dans le classement après relancement.

Des contrôles automatisés ciblent les règles de progression et le stockage des scores. Depuis la racine du projet, dans PowerShell :

```powershell
./Tests/Run-ScoreRepositoryChecks.ps1
./Tests/Run-ScoreRepositoryChecks.ps1 -ProgressionOnly
```

Si Unity est installé ailleurs, fournir son dossier `Editor/Data` avec le paramètre `-UnityData`. Ces contrôles ne testent pas les interactions physiques ou le rendu VR.

## Dépannage

**Le projet ne compile pas sur un autre ordinateur.** Vérifier en premier la version de Unity : `6000.6.0f1`. Une autre version peut modifier les API disponibles. Si une erreur mentionne un fichier absent de cette copie du projet, comparer les versions du projet échangées.

**L’arc se saisit, mais la corde ne répond pas.** Vérifier qu’une flèche est chargée près de l’encoche, que les références du prefab sont présentes et qu’il n’existe pas de composant `PullInteraction` en double.

**Les boutons sont visibles mais ne réagissent pas.** Vérifier l’EventSystem, le traitement des interactions UI du rig, le raycaster du Canvas et la portée du rayon des contrôleurs.

**Une ressource ou un prefab a disparu après transfert.** Récupérer les fichiers avec leurs `.meta` d’origine, afin de préserver les références Unity.

## Ressources et crédits

- Tutoriel de base : [Fist Full of Shrimp — Unity VR Bow and Arrow,Part 1](https://fistfullofshrimp.com/unity-vr-bow-and-arrow-part-1/).[Fist Full of Shrimp — Unity VR Bow and Arrow,Part 2](https://fistfullofshrimp.com/unity-vr-bow-and-arrow-part-2/)[Fist Full of Shrimp — Unity VR Bow and Arrow,Part 3](https://fistfullofshrimp.com/unity-vr-bow-and-arrow-part-3/)
- Modèles de l'arc et les flèches: **Fist-Full-Of-Shrimp — Bow-and-Arrow-Project-and-Assets**
- Modèle du carquois : **Dimasjk Studio — Low Poly Style Bows & Quivers Part 1**.
- Modèle des maisons : **Mikel Olaizola — Medieval_house_lite**
- Son de l'arc : **MGWSoundDesign — Bow and Hammer Sound Effects**
- Musique d’ambiance utilisée : **The Mountain  — Fantasy Ambient**.
- Frameworks et outils : Unity, XR Interaction Toolkit, OpenXR et TextMesh Pro.

Les ressources tierces restent soumises à leurs licences respectives. Cette liste décrit les ressources identifiées et doit être complétée avec les autres packs et leurs auteurs avant une redistribution publique.
