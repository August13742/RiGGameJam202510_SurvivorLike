# RiG++ 秋セメスター GameJam企画: Survivor Like (最初はそのつもり) 2025年10月10日～11月21日

### 本プロジェクトのコード(Assets/August/)はOpen Sourceである. 
Unity自体の機能以外, 外部ライブラリー一切使っておらず, すべてのコードは自作 & LLMと制作方針を討論しながら作成したものである.

#### 画像は一部AI・プログラム生成+Augustによる画像編集で作成しております. 

#### 使用するAssetはReadMe最後のリンクを参照してください(一部CC0素材以外).

#### 2025年11月21日以降の編集は, コードのリファクタリング以外行わない予定です. 

一部機能(ダイナミック武器モジュールやゴールド, プレイヤーステータスアップなど)は作ったがバランスや手間などの原因により, 本体への導入は行っていません. 

本ゲームは最初の数週間, Survivor系ゲームとして開発に進んだが, すでに4月でGodotを使って2D TopDown Survivor作ったことがあって(OpenSource, 詳細はAugust13742レポ参照), 

再度作っても何も得しない気がして, 新しく挑戦してみたく, Boss AI作ってみた結果, Survivor系ゲームの基盤でBoss戦連戦のゲームとなった. 

**~~もはやSurvivorではないと自覚している~~**

# 主要制作者 August 13742
## ボス戦Demoビデオ(一部演出のみ), 音あり
### Boss4: Eleonore
https://github.com/user-attachments/assets/c71ff0f2-0ef3-456d-b0c4-9dbc54b3b1e5

### Boss3: Fire Knight
https://github.com/user-attachments/assets/2bccb03d-eb39-4178-a228-9114a63ffbba

### Boss2: Huntress
https://github.com/user-attachments/assets/74caddc8-0620-4aa8-b79a-58eb8cecc215

### Boss1: Soldier
https://github.com/user-attachments/assets/a453402b-aedd-44c7-899d-90638397cfa3

---
## 技術的注目点(一部)
### 無限成長武器システム(`August/Weapon`) & (`August/Progression`)
攻撃力, クールダウン, 範囲, Crit率などのステータスは, すべての武器に共通し, `Base * Bonus(1.0 + %)`で実現される.

### 自作Tween ライブラリー (`August/Utility/Tween`)

### 自作CameraShake, Hitstop, Telegraph指示用ライブラリー(`August`フォルダーにあるコードはすべて自作(original))

### 共通ベースBoss Attack
すべてのボスは互いの攻撃が使え, 重み ・ ScriptableObjectで定義されているパラメーターを調整することで, パーソナリティを付けることができる.
### ダイナミック重み調整(軽め)
範囲バンドにより, 一部技は選択され難く・なくなる.

---
##### Special Thanks to:
https://chierit.itch.io/elementals-fire-knight         	(free version)\
https://luizmelo.itch.io/huntress \
https://zerie.itch.io/tiny-rpg-character-asset-pack     (free version)\
https://ragnapixel.itch.io/particle-fx 			(by Raphael Hatencia)\
https://sscary.itch.io/the-adventurer-female \
https://ansimuz.itch.io/explosion-animations-pack \
https://karsiori.itch.io/pixel-art-potion-pack-animated \
https://jellyfish0.itch.io/textures-for-particle 
https://otsoga.itch.io/free-medieval-npcs-witch-and-swordswoman

For the Assets
