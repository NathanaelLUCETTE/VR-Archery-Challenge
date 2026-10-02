# VR Archery Challenge — parcours de réalisation

Ce parcours traduit les exigences du sujet ST2OOS en étapes vérifiables. Les noms de composants et les valeurs proposés ci-dessous sont des choix de conception, pas des exigences supplémentaires du sujet.

## État initial constaté

- Projet Unity 6000.6.0f1, XR Interaction Toolkit 3.6.1 et OpenXR installés.
- `SampleScene` est activée dans la configuration de build.
- Trois scripts métier existent : `PullInteraction`, `ArrowSpawner`, `Arrow`.
- Le code calcule la tension de corde et lance une flèche avec un Rigidbody.
- Le générateur crée automatiquement une flèche : il faudra ajouter la prise manuelle demandée par le sujet.
- Le fonctionnement en casque et les références de l'Inspector restent à vérifier.
- Des modifications locales existent déjà dans les scènes et réglages : les conserver avant toute intégration.

## 1. Fiabiliser le tir existant

Objectif : saisir l'arc, tendre sa corde, tirer et recommencer sans erreur.

Points à corriger dans les scripts actuels :

- `ArrowSpawner` conserve la référence de la flèche après son départ. Lâcher l'arc peut donc détruire une flèche en vol. À la libération, détacher la référence du générateur.
- Lâcher l'arc ne réinitialise pas `_arrowNotched`. Une flèche détruite peut empêcher le rechargement suivant.
- Annuler l'apparition différée si l'arc est lâché pendant la seconde d'attente.
- Dans `PullInteraction`, protéger l'accès à `pullingInteractor` et le calcul lorsque la distance start/end est nulle.
- Dans `Arrow`, éviter une rotation vers une vitesse nulle et prévoir la destruction des projectiles après un délai.
- L'événement de tir est statique : plusieurs arcs pourraient déclencher les flèches des autres. Passer à un événement propre à chaque arc avant d'ajouter plusieurs équipements.

Dans Unity, contrôler les références `start`, `end`, `notch`, `arrow`, `tip`, le Rigidbody de la flèche et les événements de sélection de la corde. Le code existant attend les appels à `SetPullInteractor` et `Release`.

Validation : dix tirs consécutifs, puis lâcher/reprendre l'arc avant le chargement, après chargement et pendant le vol. Aucune erreur Console, aucune flèche déjà tirée supprimée par le lâcher de l'arc.

## 2. Ajouter la saisie manuelle des flèches

Prévoir une réserve de flèches saisissables et une zone d'encochage. Une flèche tenue près de l'encoche s'y attache ; la main peut ensuite tirer la corde. Seule la flèche encochée reçoit l'impulsion au relâchement.

États conseillés : disponible, tenue, encochée, en vol, plantée. Désactiver la saisie pendant le vol. Vérifier les collisions arc/flèche et joueur/flèche.

Validation : prendre une flèche avec la main libre, l'encocher, tirer ; tendre une corde sans flèche ne crée aucun projectile.

## 3. Cibles et score

Créer `TargetScore` pour convertir la position d'impact en points et `GameSession` pour cumuler les points.

- Définir un centre, un plan de cible et un rayon explicites.
- Convertir l'impact dans le repère local de la cible ; mesurer la distance au centre dans son plan.
- Exemple de cinq zones, du centre vers l'extérieur : 10, 8, 6, 4, 2 points. Hors du disque : 0.
- Évaluer chaque flèche une seule fois, même avec plusieurs colliders.
- Afficher immédiatement le score sur un Canvas World Space lisible en VR.
- Utiliser une détection continue adaptée aux flèches rapides et vérifier les traversées de cible.

Validation : centre, limite de chaque anneau, extérieur du disque, cible tournée et cible redimensionnée. Une flèche plantée ne rapporte pas de points supplémentaires.

## 4. Partie, difficulté et entraînement

Définir les états menu, entraînement, partie et résultats dans `GameSession`.

Valeurs de départ proposées, à ajuster après essai :

| Difficulté | Rayon de cible | Vitesse de déplacement | Durée |
| --- | --- | --- | --- |
| Facile | 0,60 m | 0,20 m/s | 120 s |
| Normal | 0,45 m | 0,45 m/s | 90 s |
| Difficile | 0,30 m | 0,80 m/s | 60 s |

Créer `MovingTarget` et appliquer réellement les paramètres choisis au début du niveau. Une cible mobile doit emporter ses flèches plantées.

En entraînement : cibles fixes, tirs libres, aucune limite de temps ni déblocage de progression. En partie : score, temps restant et objectif visibles ; arrêter l'attribution de points lorsque le temps est écoulé.

Validation : comparer les trois difficultés, terminer une partie, recommencer et vérifier la remise à zéro. L'entraînement ne modifie pas le classement.

## 5. Équipements et progression

Créer des ScriptableObjects `BowDefinition` et `ArrowDefinition` pour les caractéristiques et les conditions de déblocage. Prévoir au moins deux arcs et deux flèches ayant des effets mesurables sur le tir.

Par exemple : arc initial, arc amélioré ; flèche standard, flèche légère. Définir ensemble masse, impulsion et vitesse pour éviter des caractéristiques incohérentes.

Découper le jeu principal en niveaux avec un objectif de score. Atteindre l'objectif ouvre le niveau suivant et débloque un équipement. Afficher clairement les équipements verrouillés et leurs conditions.

Validation : les équipements verrouillés restent inutilisables ; un niveau réussi débloque effectivement l'amélioration annoncée.

## 6. Menu VR et audio

Menu : difficulté, sélection d'arc, sélection de flèche, volume, entraînement, démarrage et classement. Prévoir des boutons utilisables avec les contrôleurs et un retour visuel de sélection.

HUD : score, temps, niveau et objectif. Résultats : score final, difficulté, saisie du nom via clavier virtuel, enregistrement et retour au menu.

Ajouter les sons spatialisés de tension, relâchement, départ et impact, ainsi que l'ambiance. Le réglage de volume doit agir sur tous les sons concernés.

Validation : réaliser le parcours complet uniquement au casque ; vérifier lisibilité, portée des interactions et effet du volume.

## 7. Sauvegarde et classement

Créer `ScoreRepository` : liste sérialisable d'entrées contenant au minimum nom, score et difficulté. Enregistrer en JSON sous `Application.persistentDataPath`, puis recharger et trier par score décroissant.

Traiter le premier lancement sans fichier, un fichier invalide, un nom vide et les doubles clics sur Enregistrer. Utiliser des boutons de lettres, espace, effacement et validation pour le clavier VR.

Validation : enregistrer deux joueurs, relancer l'application, vérifier les données et l'ordre du classement. Vérifier aussi les noms accentués.

## 8. Livraison et démonstration

- Développer chaque fonctionnalité sur une branche dédiée, par exemple `codex/fix-arrow-lifecycle`, `codex/target-scoring`, `codex/game-session`.
- Faire des commits courts après validation ; chaque membre utilise sa propre identité Git.
- Documenter casque, version Unity, configuration et commandes dans un README.
- Tester un build sur le matériel cible, pas uniquement dans l'éditeur.
- Préparer une démonstration : entraînement, tir naturel, score central/périphérique, cible mobile, changement de difficulté, déblocage, nom au clavier et classement conservé après relancement.

Première séance conseillée : valider et corriger le tir existant (étape 1), puis intégrer une cible à score. Le matériel cible et le fonctionnement actuel du tir sont les deux informations à confirmer avant les réglages XR.
