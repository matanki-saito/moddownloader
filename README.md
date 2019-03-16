# MOD Downloader
管理対象のMODを監視して、更新があればzipをダウンロードして、exeと同じディレクトリにあるmodフォルダに展開します。

## Spec
### 設置場所
通常であれば、```C:\Users\*YOUR_USER_NAME*\Documents\Paradox Interactive\*GAME_NAME*\```においてください。
[インストーラ](https://github.com/matanki-saito/SimpleInstaller)を使うと自動でこの場所に配置します。

### 管理対象MOD
exeと同じディレクトリにclaes.keyフォルダを作り、下記の仕様でファイルを置いてください。[インストーラ](https://github.com/matanki-saito/SimpleInstaller)を使うと自動でこの場所に管理対象のMODを追加します。

 - UTF-8
 - ファイル名：GitHubのレポジトリ番号＋.key
 - １行目：Mod Name
 - ２行目：キーファイルのパス。これに紐づけて、ダウンロードするファイルが決定されます。

#### サンプル
```
CK2 JP ModCore
C:\Program Files (x86)\Steam\steamapps\common\Crusader Kings II\CK2game.exe
```

### リセット
exeと同じディレクトリにclaes.cacheフォルダがあるので、それを削除してください。

## 配信
配信に関する仕様は[こちら](https://github.com/matanki-saito/dlldistributionserver)を参照してください。
