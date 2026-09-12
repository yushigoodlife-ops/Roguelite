# 第7週：データ管理の完全版！

# 本日の目標
1. データの構造を「1ファイル＝1キャラクター」から、「1ファイル＝全員分」のリスト形式へ進化させる。
2. Excelやスプレッドシートで作ったCSVデータを、一気にScriptableObjectに変換する
3. どんなデータでも取り出せる究極の魔法のプログラム「ジェネリクス」をマスターする。

## 1.なぜCSVで管理するのか？

### 1.1 そもそも「CSV」とは？

CSV（Comma-Separated Values）とは、データをカンマ（,）で区切って並べただけの、とてもシンプルなテキストファイルのことです。<br>
メモ帳で開くと `1,Slime,10,3.5` のように文字が並んでいるだけですが、ExcelやGoogleスプレッドシートで開くと、カンマの部分で区切られてキレイな「表」として表示されます。

「誰でも表計算ソフトで簡単に編集できる」のに、「プログラムからはただの文字として簡単に読み込める」という最強のメリットがあるため、ゲーム開発をはじめ世界中のIT現場でデータ管理の標準として使われています。

### 1.2 なぜUnityで直接作らないのか？
前回、ScriptableObject（データカセット）を使って敵や武器のデータを作りました。<br>
しかし、もし敵が100種類、武器が200種類に増えたらどうなるでしょうか？ Unityの画面で1個ずつファイルを作り、数値を手入力していくのは気が遠くなる作業ですし、入力ミスも起きます。

プロのゲーム開発では、データはプランナーさんが「Excel」や「Googleスプレッドシート」でまとめ、それを `CSV` として出力します。<br>
プログラマーは、そのCSVを読み込んで「一発でゲームのデータ（ScriptableObject）に変換する魔法のツール」を自作します。今回はその魔法のツールを使ってみましょう！

### 1.3 GoogleスプレッドシートでのCSVの作り方
実際にゲームで使うCSVデータを作ってみましょう。Googleスプレッドシートを開き、以下のように入力します。
1行目（ヘッダー）: プログラムの変数名と同じ名前を英語で入力します。（例：A1に Id、B1に EnemyName、C1に MaxHp、D1に MoveSpeed）


## 2. データ構造を進化させよう（リスト化）
100体の敵のために100個のファイルを作るのは管理が大変です。<br>
「1行分のデータ（レコード）」と、それらを「リストとして全てまとめる箱（コンテナ）」の2段構えに設計し直します。

### 2.1 インターフェースの実装
まずは、全てのデータが必ず「ID」を持つことを保証するルール（インターフェース）を作ります。<br>
`Scripts/Core/` フォルダに `MasterData` フォルダを作成してください。<br>
作成した `Scripts/Core/MasterData` フォルダに `IMasterData.cs` と `IMasterDataContainer` を作成してください。<br>

**ファイル名： `IMasterData.cs`**
```cs
namespace Core.MasterData
{
    /// <summary>
    /// 1行のデータが必ずIDを持つことを保証する
    /// </summary>
    public interface IMasterData
    {
        public ulong Id { get; }
    }
}
```

**ファイル名： `IMasterData.cs`**
```cs
using System.Collections.Generic;

namespace Core.MasterData
{
    /// <summary>
    /// ScriptableObjectが必ずレコードのリストを持つことを保証する
    /// </summary>
    public interface IMasterDataContainer<T> where T : IMasterData
    {
        List<T> Records { get; }
    }
}
```

次に、敵のデータを「1行分のデータ（Record）」と「全体をまとめる箱」に分けます。<br>
前回、`InGame` フォルダ内に `EnemyData.cs` と `WeaponData.cs` を作成していましたが、それを削除してください。<br>
`Scripts/Core/MasterData/` フォルダに `Data` フォルダを作成してください。<br>
作成した `Scripts/Core/MasterData/Data` フォルダに `EnemyData` を作成してください。<br>
**ファイル名： `EnemyData.cs`**
```cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.MasterData
{
    /// <summary>
    /// CSVの1行分に相当するレコードデータ
    /// SOではなく通常のクラスにし、シリアライズ可能にする
    /// </summary>
    [Serializable]
    public class EnemyDataRecord : IMasterData
    {
        [field: SerializeField] public ulong Id { get; private set; }
        [field: SerializeField] public string EnemyName { get; private set; }
        [field: SerializeField] public int MaxHp { get; private set; }
        [field: SerializeField] public float MoveSpeed { get; private set; }
    }

    /// <summary>
    /// レコードのリストを保持する1つのSO
    /// CSVファイル名（EnemyData.csv）とこのクラス名が一致することでツールが自動認識する
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "ScriptableObjects/EnemyData")]
    public class EnemyData : ScriptableObject, IMasterDataContainer<EnemyDataRecord>
    {
        [field: SerializeField] public List<EnemyDataRecord> Records { get; private set; } = new List<EnemyDataRecord>();
    }
}
```

WeaponData も同じように、`Scripts/Core/MasterData/Data` フォルダに作成してください。<br>
**ファイル名： `WeaponData.cs`**
```cs
using System;
using System.Collections.Generic;
using System.Text;
using TPSRoguelite.InGame.Enums;
using UnityEngine;

namespace Core.MasterData
{
    [Serializable]
    public class WeaponDataRecord : IMasterData
    {
        [field: SerializeField] public ulong Id { get; private set; }

        /// <summary>
        /// 武器名
        /// </summary>
        [field: SerializeField] public string WeaponName { get; private set; }

        /// <summary>
        /// 射撃タイプ
        /// </summary>
        [field: SerializeField] public int FireType { get; private set; }

        /// <summary>
        /// 攻撃力
        /// </summary>
        [field: SerializeField] public int AttackPower { get; private set; }

        /// <summary>
        /// 撃ち終わった後のクールダウン
        /// </summary>
        [field: SerializeField] public float FireInterval { get; private set; }

        /// <summary>
        /// フルオートやバースト時の連射間隔
        /// </summary>
        [field: SerializeField] public float FireRate { get; private set; }

        /// <summary>
        /// 最大弾数
        /// </summary>
        [field: SerializeField] public int MaxAmmo { get; private set; }

        /// <summary>
        /// リロード時間
        /// </summary>
        [field: SerializeField] public float ReloadTime { get; private set; }
    }

    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "ScriptableObjects/WeaponData")]
    public class WeaponData : ScriptableObject, IMasterDataContainer<WeaponDataRecord>
    {
        [field: SerializeField] public List<WeaponDataRecord> Records { get; private set; }
    }
}
```

### 2.2 PlayerとEnemyにレコードを持たせよう
前回はDataを1行のデータとして保持していましたが、Recordがその役割をしているので置き換えましょう。<br>
恐らくエラーも出ているはずです。<br>
**ファイル名： `PlayerController.cs`**
```diff
+using Core.MasterData;
using Core.Interface;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
-using TPSRoguelite.InGame.Data;
using TPSRoguelite.InGame.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TPSRoguelite.InGame.Player {

    public class PlayerController : MonoBehaviour
    {
        /// <summary>
        /// 移動速度
        /// </summary>
        private const float MOVE_SPEED = 5.0f;

        /// <summary>
        /// 回転速度
        /// </summary>
        private const float ROTATE_SPEED = 10f;

        /// <summary>
        /// レーザーポインターの描画距離
        /// </summary>
        private const float LASER_MAX_DISTANCE = 50f;

        /// <summary>
        /// 攻撃距離（射撃範囲）
        /// </summary>
        private const float ATTACK_RANGE = 50f;

        /// <summary>
        /// 物理演算コンポーネント
        /// </summary>
        [SerializeField] private Rigidbody rigidbody;

        /// <summary>
        /// 銃口のトランスフォーム
        /// </summary>
        [SerializeField] private Transform weponOrigin;

        /// <summary>
        /// レーザーポインターの描画コンポーネント
        /// </summary>
        [SerializeField] private LineRenderer laserLineRenderer;

        /// <summary>
        /// 武器のデータ
        /// </summary>
-       [SerializeField] private WeponData currentWeapon;
+       private WeaponDataRecord currentWeapon;

        // 変数、関数省略

        private void OnFire(InputAction.CallbackContext context)
        {
            if (context.performed) 
            {
                if (!canShoot || isReloading || currentWeapon == null) 
                {
                    return;
                }
                
-               switch (currentWeapon.WeaponFireType)
+               switch ((FireType)currentWeapon.WeaponFireType)
                {
                    case FireType.SemiAuto:
                        ShootSemiAutoAsync(this.GetCancellationTokenOnDestroy()).Forget();
                        break;

                    case FireType.Burst:
                        ShootBurstAsync(this.GetCancellationTokenOnDestroy()).Forget();
                        break;

                    case FireType.FullAuto:
                        ShootFullAutoAsync(fireCts.Token).Forget();
                        break;

                    default:
                        Debug.LogWarning($"割り当てていない射撃タイプがあります。{currentWeapon.WeaponFireType}");
                        break;
                }
            }
        }

        // 関数省略
```

**ファイル名： `EnemyState.cs`**
```diff
using UnityEngine;
using UnityEngine.Events;
using Core.Interface;
-using TPSRoguelite.InGame.Data;
+using Core.MasterData;

namespace TPSRoguelite.InGame.Enemy 
{
    public class EnemyState : MonoBehaviour, IDamageable
    {
-       [field: SerializeField] public EnemyData EnemyDataAsset { get; private set; }
+       public EnemyDataRecord EnemyDataAsset { get; private set; }

        // 変数、関数省略
    }
}
```

## 3. Addressablesのインストールと初期設定

次の章で使う「変換ツール」には、Addressables（アドレッサブル）という特別な機能のプログラムが含まれています。<br>
そのため、ツールを入れる前に、まずはUnityにこの機能をインストールしておきましょう！<br>
（※Addressablesがどんな機能なのかについては、後ほど詳しく解説します！）

**【インストールと初期設定】**
1. Unity上部のメニューから `Window ＞ Package Manager` を開きます。
2. 左上の Packages: を Unity Registry に変更し、検索窓に `Addressables` と入力します。
3. `Addressables` を選択し、右下の `Install` を押してインストールします。
4. インストールが終わったら、上部メニューの `Window ＞ Asset Management ＞ Addressables ＞ Groups` を開きます。
5. 出てきたウィンドウの真ん中にある `Create Addressables Settings` というボタンをクリックします。これでパッケージの準備と初期設定は完了です！

## 4. C#の禁断の魔法「リフレクション」を使った変換ツール
指定したフォルダにあるCSVファイルを全部読み込んで、自動でScriptableObjectに変換する「エディタ拡張ツール」を導入します。

このツールの中では 「リフレクション（Reflection）」 という超強力な技を使っています。<br>
リフレクションとは、プログラムを実行している最中に「このクラスの設計図には、なんて名前の変数があるかな？」と透視したり、「private」で守られている変数に強制的に値を書き込んだりできる、ハッカーのような技術です。<br>
これを使えば、将来「アイテムデータ（ItemData.csv）」が増えても、全自動で変換に対応してくれます。

※このツールの詳しいプログラムの仕組みは、中級者向けの特別資料で別途解説します。今回はまず、用意されたツールをダウンロードして使ってみましょう！

【ツールの導入手順】
1. 「講師フォルダ」から `MasterDataImporter.cs` をダウンロードしてください。
2. UnityのProjectウィンドウで、Scripts フォルダの中に新しく `Editor` という名前のフォルダを作ってください。
**（※重要：エディタを改造するプログラムは、必ずこの `Editor` という名前のフォルダに入れないとエラーになります！）**
3. ダウンロードした `MasterDataImporter.cs` を、今作った `Editor` フォルダの中にドラッグ＆ドロップして追加します。
4. プロジェクトの `Assets/Data` フォルダの中に、新しく `CSV` という名前のフォルダを作成してください。
これでツールの準備は完了です。Unityの上部のメニューを見ると、新しく `Tools > CSVを一括でMasterDataに変換` というメニューが追加されています！<br>
先ほど作った `Assets/Data/CSV` フォルダの中に `EnemyData.csv` を入れて、このメニューを押すだけで、一瞬でデータが完成します。

## 5. 究極の汎用アクセッサ「ジェネリクス」
最後に、変換したデータをゲーム中に読み込んで取り出すシステムを作ります。<br>
ここでは 「Addressables（アドレッサブル）」 と 「ジェネリクス（Generics）」 という2つの強力な技術を使います。

### 5.1 Addressables（アドレッサブル）とは？
Unityで「ゲームの途中で必要なデータを読み込む」ための、プロの現場でよく使われる強力なシステムです。<br>
普通、Unityのデータはゲーム開始時に全部まとめてメモリに読み込まれますが、それだとスマホの容量やメモリがパンクしてしまいます。<br>
Addressablesを使うと、データに「名札（ラベル）」をつけておき、必要な時だけ「このラベルのデータを全部持ってきて！」と後から呼び出す（ロードする）ことができるようになります。

### 5.2 ジェネリクスとは？
ジェネリクスとは、メソッド名の後ろに `<T>` をつけることで、「取り扱うデータの型（敵なのか、武器なのか）を、使う時に後から決められる魔法の箱」です。<br>
これを使えば、これからどんなデータが増えても、関数を増やさずに1つの関数を使い回すことができます！

### 5.3 ジェネリクスを使った「ロード処理」の実装
まずは、Addressablesを使ってデータを一気に読み込む「ロード処理」の部分を作ります。<br>
ジェネリクス（<T>）を使うことで、敵のデータでも武器のデータでも使い回せる魔法の関数を作ります。

`Scripts/Core/MasterData` フォルダに `MasterDataAccessor.cs` を作成してください。<br>
**ファイル名： `MasterDataAccessor.cs`**
```cs
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System;
using InGame.Data;

namespace InGame.System
{
    public class MasterDataAccessor : MonoBehaviour
    {
        // Addressablesで設定するラベル
        private const string ENEMY_LABEL = "EnemyData";
        private const string WEAPON_LABEL = "WeaponData";

        /// <summary>
        /// 外部からアクセスするためのインスタンス
        /// </summary>
        public static MasterDataAccessor Instance { get; private set; }

        /// <summary>
        /// あらゆる型の辞書を「レコードの型（Type）」をキーにして一括で保持する
        /// </summary>
        private Dictionary<Type, object> masterDataDictionaries = new Dictionary<Type, object>();

        new void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start ()
        {
            InitializeAsync().Forget();
        }
        
        public async UniTask InitializeAsync()
        {
            await UniTask.WhenAll(
                // 第一引数はSOの型、第二引数はレコードの型を指定してロード！
                LoadAsync<EnemyData, EnemyDataRecord>(ENEMY_LABEL),
                LoadAsync<WeaponData, WeaponDataRecord>(WEAPON_LABEL)
            );
            
            Debug.Log("すべてのマスターデータの読み込みが完了しました。");
        }

        /// <summary>
        /// ジェネリクスを用いた汎用ロード処理
        /// TAssetはSO、TRecordはレコードデータであることをインターフェースで保証する
        /// </summary>
        private async UniTask LoadAsync<TAsset, TRecord>(string label)
            where TAsset : ScriptableObject, IMasterDataContainer<TRecord>
            where TRecord : IMasterData
        {
            var assets = await Addressables.LoadAssetsAsync<TAsset>(label, null);
            var dict = new Dictionary<ulong, TRecord>();

            foreach (var asset in assets)
            {
                // SOの中に入っているリスト（レコード群）を辞書に展開する
                foreach (var record in asset.Records)
                {
                    if (!dict.ContainsKey(record.Id))
                    {
                        dict.Add(record.Id, record);
                    }
                }
            }

            // レコードの型（TRecord）を鍵にして、完成した辞書を保存する
            masterDataDictionaries[typeof(TRecord)] = dict;
        }
    }
}
```
【ロード処理の解説】<br>
ここでは LoadAsync<TAsset, TRecord> というジェネリクス関数を作っています。<br>
Addressablesの LoadAssetsAsync を使って、指定したラベル（例えば "EnemyData"）がついているScriptableObjectをすべて探し出し、読み込んでいます。<br>
読み込んだデータは、IDを鍵（キー）とした辞書（Dictionary）に整理し、さらにその辞書自体を「レコードの型（Type）」を鍵として「辞書の辞書」に保存しています。これにより、どんなデータでも安全に保管することができます。

### 5.4 ロードが完了するまでゲームを待機させる
ゲーム開始時にマスターデータをロードする仕組みができました。しかし、ロードには数秒かかることがあります。<br>
ロードが終わっていないのにプレイヤーが動いたり、敵が出現したりするとエラーになってしまいます。 そこで、「GameManager」という司令塔を作り、「ロードが完了するまでプレイヤーの移動と敵の発生を一時停止（SetActive(false)）させる仕組み」を導入します。

司令塔の合図を待つため、`PlayerController` や `EnemySpawner` は、最初から動かないようにしておきます。<br>
**ファイル名： `PlayerController.cs`**
```diff
using Core.Interface;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
-using TPSRoguelite.InGame.Data;
using TPSRoguelite.InGame.Enums;
using UnityEngine;
using UnityEngine.InputSystem;
+using Core.MasterData;

namespace TPSRoguelite.InGame.Player {

    public class PlayerController : MonoBehaviour
    {
        // 変数省略

-       private void Awake()
+       private void Start()
        {
            gameObject.SetActive(false);

-           if (currentWeapon != null)
-           {
-               CurrentAmmo = currentWeapon.MaxAmmo;
-           }
-           else
-           {
-               Debug.LogError("currentWeaponが見つかりませんでした");
-           }

-           inputActions = new PlayerInputActions();
-           inputActions.Player.Fire.started += OnFire;
-           inputActions.Player.Fire.canceled += OnFire;
-           inputActions.Player.Reload.performed += OnReload;

-           if (UnityEngine.Camera.main != null)
-           {
-               mainCameraTransform = UnityEngine.Camera.main.transform;
-           }
-           else
-           {
-               Debug.LogError("Main Cameraが見つかりません。");
-           }
        }

+       public void Setup()
+       {
+           if (currentWeapon != null)
+           {
+               CurrentAmmo = currentWeapon.MaxAmmo;
+           }
+           else
+           {
+               Debug.LogError("currentWeaponが見つかりませんでした");
+           }

+           inputActions = new PlayerInputActions();
+           inputActions.Player.Fire.started += OnFire;
+           inputActions.Player.Fire.canceled += OnFire;
+           inputActions.Player.Reload.performed += OnReload;

+           if (UnityEngine.Camera.main != null)
+           {
+               mainCameraTransform = UnityEngine.Camera.main.transform;
+           }
+           else
+           {
+               Debug.LogError("Main Cameraが見つかりません。");
+           }

+           gameObject.SetActive(true);
+       }

        private void OnEnable()
        {
-           inputActions.Enable();            
+           inputActions?.Enable();
        }

        private void OnDisable()
        {
-           inputActions.Disable();
+           inputActions?.Disable();
        }
    }
}
```

**ファイル名： `EnemyState.cs`**
```diff
// using省略

namespace TPSRoguelite.InGame.Enemy 
{
    public class EnemyState : MonoBehaviour, IDamageable
    {
        /// <summary>
        /// 現在の体力
        /// </summary>
        public int CurrentHP { get; private set; }

        public event UnityAction<EnemyState> OnReturnToPoolAction;

-       private void OnEnable()
+       public void Setup()
        {
            if (EnemyDataAsset == null) 
            {
                Debug.LogError("EnemyDataがセットされていません。");
                return;
            }

            CurrentHP = EnemyDataAsset.MaxHP;
+           gameObject.SetActive(true);
        }
    }
}
```

**ファイル名： `EnemySpawner.cs`**
```diff
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TPSRoguelite.InGame.Enemy;
+using Core.MasterData;

namespace TPSRoguelite.InGame.Spawner 
{
    public class EnemySpawner : MonoBehaviour 
    {
        // 変数省略

-       private void Awake()
-       {
-           if (enemyPrefab == null)
-           {
-               return;
-           }
-
-           // ゲーム開始時に、あらかじめ用意した数だけ生成しておく
-           for (int i = 0; i < POOL_SIZE; i++)
-           {
-               GameObject enemyObj = Instantiate(enemyPrefab);
-               EnemyState enemy = enemyObj.GetComponent<EnemyState>();
-               if (enemy != null) 
-               {
-                   enemy.gameObject.SetActive(false);
-                   enemyPool.Enqueue(enemy);
-               }
-           }
-       }

-       private void Start()
-       {
-           SpawnLoopAsync().Forget();
-       }

+       public void Setup()
+       {
+           if (enemyPrefab == null)
+           {
+               return;
+           }
+
+           // ゲーム開始時に、あらかじめ用意した数だけ生成しておく
+           for (int i = 0; i < POOL_SIZE; i++)
+           {
+               GameObject enemyObj = Instantiate(enemyPrefab);
+               EnemyState enemy = enemyObj.GetComponent<EnemyState>();
+               if (enemy != null) 
+               if (enemy != null)
+               {
+                   enemy.gameObject.SetActive(false);
+                   enemyPool.Enqueue(enemy);
+               }
+           }
+
+           SpawnLoopAsync().Forget();
+       }

        /// <summary>
        /// 敵の生成
        /// </summary>
        private void SpawnEnemyFromPool()
        {
            if (enemyPrefab == null || spawnPoints.Length == 0)
            {
                return;
            }

            // ランダムな出現場所を決める
            int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomIndex];

            Vector3 safePosition = spawnPoint.position;

            // 選んだポイントの周囲にNavMeshがあるか探す
            if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, MAX_SPAWN_DISTANCE, NavMesh.AllAreas)) 
            {
                // 見つかったら、安全な座標で上書きする
                safePosition = hit.position;
            }
            else
            {
                // 見つからなかったら、生成を諦める
                Debug.LogWarning("近くに安全なスポーン位置が見つかりませんでした。");
                return;
            }

            EnemyState enemy = null;
            if (enemyPool.Count > 0)
            {
                enemy = enemyPool.Dequeue();
            }
            else
            {
                Debug.LogWarning("プールに空きがなかったため、Instantiateで生成します。プールのサイズを増やすか、生成に制限をかけてください");
                GameObject enemyObj = Instantiate(enemyPrefab);
                enemy = enemyObj.GetComponent<EnemyState>();
                if (enemy == null)
                {
                    Debug.LogError("EnemyStateの取得に失敗しました。");
                    return;
                }
            }

            enemy.OnReturnToPoolAction -= ReturnToPool;
            enemy.OnReturnToPoolAction += ReturnToPool;

            enemy.transform.position = safePosition;
            enemy.transform.rotation = spawnPoint.rotation;

-           enemy.gameObject.SetActive(true);
+           enemy.Setup();
        }
        
        /// <summary>
        /// プールへ戻す
        /// </summary>
        private void ReturnToPool(EnemyState enemy)
        {
            enemyPool.Enqueue(enemy);
            enemy.OnReturnToPoolAction -= ReturnToPool;
        }
    }
}
```

`Scripts/InGame` フォルダに `Manager` フォルダを作成してください。<br>
作成した `Scripts/InGame/Manager` フォルダに `GameManager.cs` を作成してください。<br>
**ファイル名： `GameManager.cs`**
```cs
using Core.MasterData;
using Cysharp.Threading.Tasks;
using TPSRoguelite.InGame.Player;
using TPSRoguelite.InGame.Spawner;
using UnityEngine;

namespace TPSRoguelite.InGame.Manager
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private PlayerController player = null;
        [SerializeField] private EnemySpawner enemySpawner = null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 非同期でセットアップを開始する
            Setup().Forget();
        }

        private async UniTaskVoid Setup()
        {
            // 【重要】ここでマスターデータの読み込みが完了するまで「待つ（await）」！
            await MasterDataAccessor.Instance.InitializeAsync();

            // 読み込みが完了したら、プレイヤーと敵発生装置の準備を始める
            if (player != null)
            {
                player.Setup();
            }

            if (enemySpawner != null)
            {
                enemySpawner.Setup();
            }
        }
    }
}
```

### 5.5 データの「取り出し方」の実装
**ファイル名： `MasterDataAccessor.cs` に追記（取り出し処理）**
```cs
namespace InGame.System
{
    public class MasterDataAccessor : MonoBehaviour
    {
        // （上のロード処理は省略）

        // ==========================================
        // データの取り出し方
        // ==========================================

        /// <summary>
        /// 型とIDを指定して、該当するマスターデータを1つ取得する
        /// 使い方： accessor.GetById<EnemyDataRecord>(101);
        /// </summary>
        public TRecord GetById<TRecord>(ulong id) where TRecord : IMasterData
        {
            if (masterDataDictionaries.ContainsKey(typeof(TRecord)))
            {
                var dict = (Dictionary<ulong, TRecord>)masterDataDictionaries[typeof(TRecord)];
                if (dict.ContainsKey(id))
                {
                    return dict[id];
                }
            }

            Debug.LogWarning($"{typeof(TRecord).Name}にID:{id}が見つかりません。");
            return default;
        }

        /// <summary>
        /// 型を指定して、その型のすべてのマスターデータを取得する
        /// 使い方： foreach(var enemy in accessor.GetAll<EnemyDataRecord>()) { ... }
        /// </summary>
        public IReadOnlyCollection<TRecord> GetAll<TRecord>() where TRecord : IMasterData
        {
            if (masterDataDictionaries.ContainsKey(typeof(TRecord)))
            {
                var dict = (Dictionary<ulong, TRecord>)masterDataDictionaries[typeof(TRecord)];
                return dict.Values;
            }

            return new TRecord[0];
        }

        /// <summary>
        /// 型を指定して、該当するマスターデータをランダムで1つ取得する
        /// </summary>
        public TRecord GetRandom<TRecord>() where TRecord : IMasterData
        {
            if (masterDataDictionaries.ContainsKey(typeof(TRecord)))
            {
                var dict = (Dictionary<ulong, TRecord>)masterDataDictionaries[typeof(TRecord)];
                int randomIndex = UnityEngine.Random.Range(0, Count<TRecord>());
                return GetAll<TRecord>().ToList()[randomIndex];
            }

            return default;
        }

        /// <summary>
        /// 型を指定して、条件に当てはまるデータを渡す
        /// </summary>
        public IEnumerable<TRecord> Where<TRecord>(Func<TRecord, bool> predicate)
        {
            if (masterDataDictionaries.ContainsKey(typeof(TRecord)))
            {
                var dict = (Dictionary<ulong, TRecord>)masterDataDictionaries[typeof(TRecord)];
                return dict.Where(v => predicate(v.Value)).Select(vv => vv.Value);
            }

            return Enumerable.Empty<TRecord>();
        }

        /// <summary>
        /// 型を指定して、条件に当てはまるデータがある場合、一番先頭のデータを渡す
        /// </summary>
        public TRecord First<TRecord>(Func<TRecord, bool> predicate = null)
        {
            if (masterDataDictionaries.ContainsKey(typeof(TRecord)))
            {
                var dict = (Dictionary<ulong, TRecord>)masterDataDictionaries[typeof(TRecord)];
                return dict.FirstOrDefault(v => predicate?.Invoke(v.Value) ?? true).Value;
            }

            return default;
        }

        /// <summary>
        /// 型を指定して、条件に当てはまるデータがあるかをtrueかfalseで返す
        /// </summary> 
        public bool Any<TRecord>(ulong id)
        {
            if (masterDataDictionaries.ContainsKey(typeof(TRecord)))
            {
                var dict = (Dictionary<ulong, TRecord>)masterDataDictionaries[typeof(TRecord)];
                return dict.ContainsKey(id);
            }

            return false;
        }

        public bool Any<TRecord>(Func<TRecord, bool> predicate)
        {
            if (masterDataDictionaries.ContainsKey(typeof(TRecord)))
            {
                var dict = (Dictionary<ulong, TRecord>)masterDataDictionaries[typeof(TRecord)];
                return dict.Any(v => predicate.Invoke(v.Value));
            }

            return false;
        }

        /// <summary>
        /// 型を指定して、その型のすべてのマスターデータの数を取得する
        /// </summary>
        public int Count<TRecord>() where TRecord : IMasterData
        {
            if (masterDataDictionaries.ContainsKey(typeof(TRecord)))
            {
                var dict = (Dictionary<ulong, TRecord>)masterDataDictionaries[typeof(TRecord)];
                return dict.Count;
            }

            return 0;
        }

        public int Count<TRecord>(Func<TRecord, bool>predicate)
        {
            if (masterDataDictionaries.ContainsKey(typeof(TRecord)))
            {
                var dict = (Dictionary<ulong, TRecord>)masterDataDictionaries[typeof(TRecord)];
                return dict.Count(v => predicate.Invoke(v.Value));
            }

            return 0;
        }
    }
}
```

## 5.6 マスターデータを実際に使ってみよう！
この究極のアクセッサを使って、敵が生成されたときにマスターデータから自分の能力（HPや速度）を引っ張ってくる例を見てみましょう。<br>
**ファイル名： `EnemyState.cs`**
```diff
// using省略

namespace TPSRoguelite.InGame.Enemy 
{
    public class EnemyState : MonoBehaviour, IDamageable
    {
        /// <summary>
        /// 現在の体力
        /// </summary>
        public int CurrentHP { get; private set; }

        public event UnityAction<EnemyState> OnReturnToPoolAction;

+       public void Initialize(ulong id)
+       {
+           EnemyDataAsset = MasterDataAccessor.Instance.GetById<EnemyDataRecord>(id);
+       }

        public void Setup()
        {
            if (EnemyDataAsset == null) 
            {
                Debug.LogError("EnemyDataがセットされていません。");
                return;
            }

            CurrentHP = EnemyDataAsset.MaxHp;
            gameObject.SetActive(true);
        }
    }
}
```

**ファイル名： `EnemySpawner.cs`**
```diff
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TPSRoguelite.InGame.Enemy;
using Core.MasterData;

namespace TPSRoguelite.InGame.Spawner 
{
    public class EnemySpawner : MonoBehaviour 
    {
        // 変数省略

        public void Setup()
        {
            if (enemyPrefab == null)
            {
                return;
            }

            // ゲーム開始時に、あらかじめ用意した数だけ生成しておく
            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject enemyObj = Instantiate(enemyPrefab);
                EnemyState enemy = enemyObj.GetComponent<EnemyState>();
                if (enemy != null) 
                if (enemy != null)
                {
+                   ulong randomId = (ulong)UnityEngine.Random.Range(1, MasterDataAccessor.Instance.Count<EnemyDataRecord>());
+                   enemy.Initialize(randomId);
                    enemy.gameObject.SetActive(false);
                    enemyPool.Enqueue(enemy);
                }
            }

            gameObject.SetActive(true);
            SpawnLoopAsync().Forget();
        }
    }
}
```

Playerの武器もインスペクターからIDを設定して、設定したIDの武器を装備できるようにしましょう。<br>
**ファイル名： `PlayerController.cs`**
```diff
using Core.MasterData;
using Core.Interface;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TPSRoguelite.InGame.Data;
using TPSRoguelite.InGame.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TPSRoguelite.InGame.Player {

    public class PlayerController : MonoBehaviour
    {
        /// <summary>
        /// 移動速度
        /// </summary>
        private const float MOVE_SPEED = 5.0f;

        /// <summary>
        /// 回転速度
        /// </summary>
        private const float ROTATE_SPEED = 10f;

        /// <summary>
        /// レーザーポインターの描画距離
        /// </summary>
        private const float LASER_MAX_DISTANCE = 50f;

        /// <summary>
        /// 攻撃距離（射撃範囲）
        /// </summary>
        private const float ATTACK_RANGE = 50f;

        /// <summary>
        /// 物理演算コンポーネント
        /// </summary>
        [SerializeField] private Rigidbody rigidbody;

        /// <summary>
        /// 銃口のトランスフォーム
        /// </summary>
        [SerializeField] private Transform weponOrigin;

        /// <summary>
        /// レーザーポインターの描画コンポーネント
        /// </summary>
        [SerializeField] private LineRenderer laserLineRenderer;

+       /// <summary>
+       /// 武器のID（デフォルトは1）
+       /// </summary>
+       [SerializeField] private ulong weaponId = 1;

        // 変数省略

        private void Start()
        {
            gameObject.SetActive(false);
        }

        public void Setup()
        {
+           currentWeapon = MasterDataAccessor.Instance.GetById<WeaponDataRecord>(weaponId);

            if (currentWeapon != null)
            {
                CurrentAmmo = currentWeapon.MaxAmmo;
            }
            else
            {
                Debug.LogError("currentWeaponが見つかりませんでした");
            }

            inputActions = new PlayerInputActions();
            inputActions.Player.Fire.started += OnFire;
            inputActions.Player.Fire.canceled += OnFire;
            inputActions.Player.Reload.performed += OnReload;

            if (UnityEngine.Camera.main != null)
            {
                mainCameraTransform = UnityEngine.Camera.main.transform;
            }
            else
            {
                Debug.LogError("Main Cameraが見つかりません。");
            }

            gameObject.SetActive(true);
        }

        // 関数省略
    }
}
```

### 5.7 GameManagerとAccessorをシーンに配置する
最後に、作成したシステムを動かすための仕上げをします。
1. UnityのHierarchyウィンドウで右クリックし、`Create Empty` を選んで空のオブジェクトを作ります。
2. 名前を `GameManager` 変更します。
3. このオブジェクトに、今回作った `MasterDataAccessor` と `GameManager` の2つのスクリプトをドラッグ＆ドロップしてアタッチします。
4. `GameManager` コンポーネントの `Player` と `Enemy Spawner` の枠に、シーン上にあるプレイヤーと敵発生装置をそれぞれドラッグ＆ドロップしてセットします。