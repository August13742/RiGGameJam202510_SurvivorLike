# RiG++ 秋セメスター GameJam企画: Vampire-Survivor Like (最初はそのつもり) Snapshot 11月16日

本プロジェクトのコード(Assets/August/)はOpen Sourceである

## 主要制作者 August 13742
### Boss3: Fire Knight
https://github.com/user-attachments/assets/210fe5a7-d5e3-4eb8-8179-8ef7a0b37de2

### Boss2: Huntress
https://github.com/user-attachments/assets/f0900a6f-9a4a-4a7f-b9fa-f24a31f4f257

### Boss1: Soldier
https://github.com/user-attachments/assets/2499db02-f374-4282-9ad3-88a6ecd5aff7

## 技術的注目点(一部)
### 無限成長武器システム(`August/Weapon`) & (`August/Progression`)
攻撃力, クールダウン, 範囲, Crit率などのステータスは, すべての武器に共通し, `Base * Bonus(1.0 + %)`で実現される.

### 自作Tween ライブラリー (`August/Utility/Tween`)

### 自作CameraShake, Hitstop, Telegraph指示用ライブラリー

### 共通ベースBoss Attack
すべてのボスは互いの攻撃が使え, 重み ・ ScriptableObjectで定義されているパラメーターを調整することで, パーソナリティを付けることができる.


credits:
https://chierit.itch.io/elementals-fire-knight         	(free version)
https://luizmelo.itch.io/huntress
https://zerie.itch.io/tiny-rpg-character-asset-pack     (free version)
https://ragnapixel.itch.io/particle-fx 			(by Raphael Hatencia)
https://sscary.itch.io/the-adventurer-female
https://ansimuz.itch.io/explosion-animations-pack
https://karsiori.itch.io/pixel-art-potion-pack-animated
https://jellyfish0.itch.io/textures-for-particles
