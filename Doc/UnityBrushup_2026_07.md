# 第6週：プログラムとデータを分離せよ！「ScriptableObject」完全実装

## 本日の目標
1. `ScriptableObject` を使って、武器や敵の設計図（データカセット）を作る
2. `PlayerController` を改造し、カセットを差し替えるだけで「フルオート」や「バースト」など、銃の性能とマガジン管理が完全に切り替わるシステムを作る

## 敵のデータカセットを作ろう
これまでのプログラムでは、敵のHPを `const int MAX_HP = 100;` のようにコードの中に直接書いていました（ハードコード）。これではHPの違う敵を量産できません。

量産できるように、プログラムとデータを完全に分けて作ります。イメージとしては、「ゲーム機本体（プログラム）」と「ゲームカセット（データ）」の関係ですね。<br>
まずは敵のカセットを作りましょう。
`Scripts/InGame` に `Data` フォルダを作成し、そのフォルダで `EnemyData.cs` を作成します。

**ファイル名： `EnemyData.cs`**
``` cs
using UnityEngine;

namespace InGame.Data
{
    // 右クリックメニューからこのデータを作成できるようにするための記述
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "ScriptableObjects/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        // [field: SerializeField] をつけると、private set なのにUnityの画面（Inspector）からは編集できるようになります。

        /// <summary>
        /// 敵の名前
        /// </summary>
        [field: SerializeField] public string EnemyName { get; private set; }

        /// <summary>
        /// 最大HP
        /// </summary>
        [field: SerializeField] public int MaxHp { get; private set; }

        /// <summary>
        /// 移動速度
        /// </summary>
        [field: SerializeField] public float MoveSpeed { get; private set; }
    }
}

```

スクリプトを保存したら、データを格納するフォルダを作成します。<br>
UnityのProjectウィンドウで右クリックし、`Create ＞ Folder` を選びます。名前を `Data` とします。<br>
さらに、Dataフォルダから、 `Create ＞ Folder` を選び、`Enemy` というフォルダを作成します。<br>
Enemyフォルダで右クリックし、 `Create ＞ ScriptableObjects ＞ EnemyData` を選びます。これでHPや速度を自由に入力できるカセットが完成しました！

## 2. 敵のデータを完璧に反映させよう
作ったカセットを敵のプログラムに読み込ませます。

**ファイル名： `EnemyState.cs` （一部改修）**
``` diff
using UnityEngine;
using UnityEngine.Events;
+ using InGame.Data; // EnemyDataを使うために追加

namespace InGame.Enemy
{
    public class EnemyState : MonoBehaviour, IDamageable
    {
-       private const int MAX_HP = 100; // 直接数字を書くのはやめるので削除！

+       // Inspectorでカセットをセットしつつ、他のプログラムからは「読み取り（Get）」だけできるように公開する
+       [field: SerializeField] public EnemyData EnemyDataAsset { get; private set; }

        public int CurrentHP { get; private set; }

        public event UnityAction<EnemyState> OnReturnToPoolAction;

-       private void Awake() 
-       {
-           CurrentHP = MAX_HP;
-       }

        private void OnEnable()
        {
+           // カセットがセットされていれば、そのカセットの最大HPを読み込む
+           if (EnemyDataAsset != null)
+           {
+               CurrentHP = EnemyDataAsset.MaxHp;
+           }
+           else
+           {
+               Debug.LogError("EnemyDataがセットされていません！");
+           }
        }
        
        public void TakeDamage(int damageAmount) 
        {
            // マイナスのダメージ（回復）を防ぐ
            if (damageAmount <= 0)
            {
                return;
            }

            CurrentHP -= damageAmount;
-           Debug.Log($"敵に{damageAmount}のダメージ！残りHP:{CurrentHP}");
+           Debug.Log($"{EnemyDataAsset.EnemyName}に{damageAmount}のダメージ！残りHP:{CurrentHP}");
        }

        private void Die() 
        {
-           Debug.Log("敵を倒しました");
+           Debug.Log($"{EnemyDataAsset.EnemyName}を倒しました");
            gameObject.SetActive(false);
            OnReturnToPoolAction?.Invoke(this);
        }
    }
}
```

**ファイル名： `EnemyController.cs` （一部改修）**
``` diff
using UnityEngine;
using UnityEngine.AI;

namespace InGame.Enemy
{  
    public class EnemyController : MonoBehaviour
    {
        private const string PLAYER_TAG_NAME = "Player";
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private EnemyState enemyState;
        private Transform targetPlayer;

        private void Awake()
        {
            // シーンから"Player"というタグが付いたオブジェクトを探す
            GameObject player = GameObject.FindGameObjectWithTag(PLAYER_TAG_NAME);
            if (player != null) 
            {
                targetPlayer = player.transform;
            }
            else
            {
                Debug.LogError($"{PLAYER_TAG_NAME}というタグのついたオブジェクトが見つかりませんでした。");
            }

+           // EnemyState が持っているカセットから、速度のデータを読み取ってナビにセットする！
+           if (enemyState != null && enemyState.EnemyDataAsset != null)
+           {
+               navMeshAgent.speed = enemyState.EnemyDataAsset.MoveSpeed;
+           }
        }
        // （Updateの追跡処理はそのまま）
    }
}
```
最後に、Unity画面で敵の `EnemyState` コンポーネントにある「Enemy Data Asset」の枠に、作ったカセットをドラッグ＆ドロップしてください。これでHPと速度が完璧に連動します！

## 3. 武器のデータと「連射タイプ」を作ろう
次は武器です。「1回押すと1発出る（セミオート）」「押しっぱなしで連射（フルオート）」「1回押すと3発出る（バースト）」という種類をUnityの画面で選べるようにするため、「列挙型（enum）」というテクニックを使います。
`Scripts/InGame` に `Enums` フォルダを作成し、そのフォルダで `WeaponEnum.cs` を作成します。

**ファイル名： `WeaponEnum.cs`**
``` cs
namespace InGame.Enums
{
    /// <summary>
    /// 武器の種類を定義する列挙型
    /// </summary>
    public enum FireType
    {
        SemiAuto = 0,  // セミオート（単発）
        Burst = 1,     // 3点バースト
        FullAuto = 2,  // フルオート（連射）
    }
}
```

`Scripts/InGame/Data` フォルダに `WeaponData.cs` を作成します。
``` cs
using UnityEngine;
using InGame.Enums;

namespace InGame.Data
{
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "ScriptableObjects/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        /// <summary>
        /// 武器の名前
        /// </summary>
        [field: SerializeField] public string WeaponName { get; private set; }

        /// <summary>
        /// 連射タイプ
        /// </summary>
        [field: SerializeField] public FireType WeaponFireType { get; private set; }

        /// <summary>
        /// 攻撃力
        /// </summary>
        [field: SerializeField] public int AttackPower { get; private set; }
        
        /// <summary>
        /// フルオートやバースト時の連射間隔
        /// </summary>
        [field: SerializeField] public float FireInterval { get; private set; }

        /// <summary>
        /// 次の球が撃てるまでの待機時間
        /// </summary>
        [field: SerializeField] public float FireRate { get; private set; }

        /// <summary>
        /// マガジンの最大弾数
        /// </summary>
        [field: SerializeField] public int MaxAmmo { get; private set; }
        
        /// <summary>
        /// リロードにかかる時間
        /// </summary>
        [field: SerializeField] public float ReloadTime { get; private set; }
    }
}
```

## 4.PlayerControllerで射撃システムを実装する
皆さんのプレイヤー（`PlayerController`）に、この武器データを読み込んで射撃を行うシステムを組み込みます。<br>
今回、フルオート、バースト、セミオートの3種類を作ろうと思います。一気に書くのは大変なので、シンプルなセミオートから作ります。<br>
また、銃を撃ったときに弾切れだった場合、勝手にリロードする処理も作ります。

**ファイル名： `PlayerController.cs`（射撃機能の追加）**
``` diff
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
+using InGame.Data; // 武器データを使うために追加

namespace InGame.Player
{
    public class PlayerController : MonoBehaviour
    {
-       /// <summary>
-       /// 相手に与えるダメージ量
-       /// </summary>
-       private const int ATTACK_DAMAGE = 20;

        /// <summary>
        /// 攻撃距離（射撃範囲）
        /// </summary>
        private const float ATTACK_RANGE = 50f;

-       /// <summary>
-       /// 最大弾数
-       /// </summary>
-       private const int MAX_AMMO = 30;

-       /// <summary>
-       /// リロード時間
-       /// </summary>
-       private const float RELOAD_TIME = 1.5f;

        /// <summary>
        /// 物理演算コンポーネント
        /// </summary>
        [SerializeField] private Rigidbody rigidbody;

        (変数定義している箇所を省く)

        /// <summary>
        /// レーザーポインターの描画コンポーネント
        /// </summary>
        [SerializeField] private LineRenderer laserLineRenderer;

+       /// <summary>
+       /// 武器のデータ
+       /// </summary>
+       [SerializeField] private WeaponData currentWeapon;

        /// <summary>
        /// リロードしているか
        /// </summary>
        private bool isReloading;

+       /// <summary>
+       /// 射撃可能か
+       /// </summary>
+       private bool canShoot = true;

        private void Awake()
        {
-           CurrentAmmo = MAX_AMMO;
+           // ゲーム開始時に、マガジンに弾をフル装填する
+           if (currentWeapon != null)
+           {
+               CurrentAmmo = currentWeapon.MaxAmmo;
+           }
+           else
+           {
+               Debug.LogError("currentWeaponが見つかりませんでした");
+           }

+           inputActions = new PlayerInputActions();
-           inputActions.Player.Fire.performed += OnFire;
+           inputActions.Player.Fire.started += OnFire;
+           inputActions.Player.Reload.performed += OnReload;
        }

        （OnEnableやOnDisableは省略）

        private void Update()
        {
            （処理省略）
        }

        public void OnFire(InputAction.CallbackContext context)
        {
+           if (context.started)
+           {
+               // クールダウン中やリロード中（撃てない状態）なら、連打されても完全に無視する！
+               if (!canShoot || isReloading || currentWeapon == null)
+               {
+                   return;
+               }
+
+               switch(currentWeapon.WeaponFireType)
+               {
+                   case FireType.SemiAuto:
+                       // セミオートは指を離しても中断しないので、消滅トークンだけ渡す
+                       ShootSemiAutoAsync(this.GetCancellationTokenOnDestroy()).Forget();
+                       break;
+               }
+           }
        }

+       /// <summary>
+       /// セミオートの射撃処理
+       /// </summary>
+       private async UniTaskVoid ShootSemiAutoAsync (CancellationToken token)
+       {
+           canShoot = false;

+           if (CurrentAmmo <= 0)
+           {
+               ReloadAsync().Forget();
+               return;
+           }

+           CurrentAmmo--;
+           Debug.Log($"バン！ 残弾: {CurrentAmmo}");
+           Shoot();

+           await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: token);

+           canShoot = true;
        }

+       /// <summary>
+       /// 共通の射撃処理
+       /// </summary>
+       private void Shoot()
+       {
+           Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);

+           // 光線に何かが当たったか判定
+           if (Physics.Raycast(ray, out RaycastHit hitInfo, ATTACK_RANGE))
+           {
+               Debug.Log($"{hitInfo.collider.name}に命中！");

+               // 当たった相手が IDamageable を持っているか確認
+               IDamageable target = hitInfo.collider.GetComponent<IDamageable>();

+               // ダメージを受ける性質を持ったオブジェクトであればダメージを与える
+               if (target != null)
+               {
+                   target.TakeDamage(currentWeapon.AttackPower);
+               }
+           }
+        }

        private async UniTaskVoid ReloadAsync()
        {
-           if (isReloading || CurrentAmmo == MAX_AMMO) 
+           if (isReloading || CurrentAmmo == currentWeapon.MaxAmmo)
            {
                return;
            }

            isReloading = true;
            Debug.Log("リロード開始...");

            // 武器データに設定された時間だけ待つ
-           await UniTask.Delay(TimeSpan.FromSeconds(RELOAD_TIME), cancellationToken: this.GetCancellationTokenOnDestroy());
+           await UniTask.Delay(System.TimeSpan.FromSeconds(currentWeapon.ReloadTime), cancellationToken: this.GetCancellationTokenOnDestroy());

-           CurrentAmmo = MAX_AMMO;
+           CurrentAmmo = currentWeapon.MaxAmmo;
            isReloading = false;
            Debug.Log("リロード完了！");
        }
    }
}
```

スクリプトを保存したら、UnityのProjectウィンドウのDataフォルダに移動、右クリックし、 `Create ＞ Folder` を選び、名前を `Weapon` にします。<br>
Weaponフォルダで `Create ＞ ScriptableObjects ＞ WeaponData` を選び、名前を `Handgun` にします。<br>
WeaponNameやAttackPowerなどは自由にしてもらって構いません。FireTypeだけ必ず `SemiAuto` にしてください。<br>
最後に、Prefabフォルダにある `Player.prefab` の `PlayerController.cs` コンポーネントにある `currentWeapon` の枠に、作ったカセットをドラッグ＆ドロップしてください。
Playを押し、左クリックでセミオートの武器で撃っていることを、Consoleウィンドウで「バン！ 残弾:○○」確認してください。

次にバーストの実装をします。
**ファイル名： `PlayerController.cs`（射撃機能の追加）**
``` diff
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using InGame.Data;

namespace InGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        （既に作っている変数や関数は省略）

        private void Update()
        {
            （処理省略）
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                // クールダウン中やリロード中（撃てない状態）なら、連打されても完全に無視する！
                if (!canShoot || isReloading || currentWeapon == null)
                {
                    return;
                }
 
                switch(currentWeapon.WeaponFireType)
                {
                    case FireType.SemiAuto:
                        // セミオートは指を離しても中断しないので、消滅トークンだけ渡す
                        ShootSemiAutoAsync(this.GetCancellationTokenOnDestroy()).Forget();
                        break;
+
+                   case FireType.Burst:
+                       // バーストも途中で止まらないように消滅トークンだけ渡す
+                       ShootBurstAsync(this.GetCancellationTokenOnDestroy()).Forget();
+                       break;
                }
            }
        }

        private async UniTaskVoid ShootSemiAutoAsync (CancellationToken token)
        {
            （処理省略）
        }

+       /// <summary>
+       /// バーストの処理
+       /// </summary>
+       private async UniTaskVoid ShootBurstAsync (CancellationToken token)
+       {
+           canShoot = false;
+           for (int i = 0; i < 3; i++)
+           {
+               if (CurrentAmmo <= 0)
+               {
+                   canShoot = true;
+                   return;
+               }
+
+               CurrentAmmo--;
+               Shoot();
+               Debug.Log($"バースト！ 残弾: {CurrentAmmo}");
+
+               await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInterval), cancellationToken: token);
+           }
+
+           await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: token);
+           canShoot = true;
+       }
    }
}
```

Weaponフォルダで `Create ＞ ScriptableObjects ＞ WeaponData` を選び、名前を `Burstgun` にします。<br>
WeaponNameやAttackPowerなどは自由にしてもらって構いません。FireTypeだけ必ず `Burst` にしてください。<br>
最後に、Prefabフォルダにある `Player.prefab` の `PlayerController.cs` コンポーネントにある `currentWeapon` の枠に、作ったカセットをドラッグ＆ドロップしてください。
Playを押し、左クリックでバーストの武器で撃っていることを、Consoleウィンドウで「バースト！ 残弾:○○」確認してください。

最後のフルオートを作成します。

**ファイル名： `PlayerController.cs`（射撃機能の追加）**
``` diff
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Core.Interface;
using InGame.Data;

namespace InGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        （既に作っている変数は省略）

        /// <summary>
        /// リロードしているか
        /// </summary>
        private bool isReloading;

        /// <summary>
        /// 射撃可能か
        /// </summary>
        private bool canShoot = true;

+       /// <summary>
+       /// 射撃のキャンセルトークン
+       /// </summary>
+       private CancellationTokenSource fireCts;

        /// <summary>
        /// 外部（アニメーションやUIなど）に現在の速度を教えるために保持するVelocity
        /// </summary>
        public Vector3 CurrentVelocity { get; private set; }

        /// <summary>
        /// 現在の弾数
        /// </summary>
        public int CurrentAmmo { get; private set; }

        （既に作っている変数は省略）

         private void Awake()
        {
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
+           inputActions.Player.Fire.canceled += OnFire;
            inputActions.Player.Reload.performed += OnReload;

            if (UnityEngine.Camera.main != null)
            {
                mainCameraTransform = UnityEngine.Camera.main.transform;
            }
            else
            {
                Debug.LogError("Main Cameraが見つかりません。");
            }
        }

        private void Update()
        {
            （処理省略）
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                // クールダウン中やリロード中（撃てない状態）なら、連打されても完全に無視する！
                if (!canShoot || isReloading || currentWeapon == null)
                {
                    return;
                }

+               // 押された瞬間に、新しいキャンセルスイッチを作成
+               fireCts = new CancellationTokenSource();
+
+               // プレイヤーが消滅した時と、ボタンを離した時のトークンを合体させる
+               var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(fireCts.Token, this.GetCancellationTokenOnDestroy());
 
                switch(currentWeapon.WeaponFireType)
                {
                    case FireType.SemiAuto:
                        // セミオートは指を離しても中断しないので、消滅トークンだけ渡す
                        ShootSemiAutoAsync(this.GetCancellationTokenOnDestroy()).Forget();
                        break;

                    case FireType.Burst:
                        // バーストも途中で止まらないように消滅トークンだけ渡す
                        ShootBurstAsync(this.GetCancellationTokenOnDestroy()).Forget();
                        break;

+                   case FireType.FullAuto:
+                       // フルオートは指を離した時に止めるため、合体させたトークンを渡す
+                       ShootFullAutoAsync(linkedCts.Token).Forget();
+                       break;
                }
            }

+           // ボタンが離れたときに、フルオートのループを解除するために、キャンセルトークンのキャンセル処理を行う
+           if (context.canceled)
+           {
+               fireCts?.Cancel();
+               fireCts?.Dispose();
+               fireCts = null;
+           }
        }

        private async UniTaskVoid ShootSemiAutoAsync (CancellationToken token)
        {
            （処理省略）
        }

        private async UniTaskVoid ShootBurstAsync (CancellationToken token)
        {
            （処理省略）
        }

+       /// <summary>
+       /// フルオートの処理
+       /// </summary>
+       private async UniTaskVoid ShootFullAutoAsync (CancellationToken token)
+       {
+           canShoot = false;
+
+           while (!token.IsCancellationRequested)
+           {
+               if (CurrentAmmo <= 0)
+               {
+                   ReloadAsync().Forget();
+                   break;
+               }
+
+               CurrentAmmo--;
+               Debug.Log($"フルオート発射！ 残弾: {CurrentAmmo}");
+               Shoot();
+
+               bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInterval), cancellationToken: token).SuppressCancellationThrow();
+
+               if (isCanceled)
+               {
+                   break;
+               }
+           }
+
+           await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: this.GetCancellationTokenOnDestroy());
+
+           canShoot = true;
+       }
    }
}
```

Weaponフォルダで `Create ＞ ScriptableObjects ＞ WeaponData` を選び、名前を `FullAuto` にします。<br>
WeaponNameやAttackPowerなどは自由にしてもらって構いません。FireTypeだけ必ず `FullAuto` にしてください。<br>
最後に、Prefabフォルダにある `Player.prefab` の `PlayerController.cs` コンポーネントにある `currentWeapon` の枠に、作ったカセットをドラッグ＆ドロップしてください。
Playを押し、左クリック長押しするとConsoleウィンドウで「フルオート発射！ 残弾:○○」と表示されていることを確認してください。