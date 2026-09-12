# 第9週：エフェクトとUIで「ゲームの爽快感」を極める！

## 本日の目標
今日は、ただ動くだけのプログラムを「触っていて気持ちいいゲーム」へと進化させる「フィードバック（反応）」と「カメラワーク」の作り方を学びます！
1. **カメラ調整**：TPSらしいカメラ視点に調整する
2. **撃つ快感（射撃エフェクト）**：弾が出た瞬間の火花（マズルフラッシュ）を作り、「撃った感触」を演出する。
3. **当てる快感（ヒットリアクション）**：敵が赤く点滅し、後ろに弾き飛ぶことで「攻撃が効いている！」という手触りを作る。
4. **迷わせない配慮（武器UI）**：残弾数や射撃モード（SEMI/AUTO）を画面に出し、プレイヤーのストレスを無くす。
5. **魔法のツール（DOTween）**：ツールを使い、たった1行でリロードUIを動かす。
   
これらを実装することで、ただのプログラムの塊が「触っていて気持ちいいゲーム」に生まれ変わります！

## 1.カメラ調整
今のカメラ視点は少し見づらく、キャラクターの動きもぎこちないですよね。<br>
TPSゲームのような「肩越しの視点」に変え、キャラクターが常にカメラの向いている方向を向くように修正しましょう！

### 1-1 スクリプトによるカメラ制御
前と後ろの計算だけでなく、左右の計算（Shoulder Offset）を足すことで、カッコいい肩越しの視点が実現できます。

**ファイル名： `CameraController.cs`**
``` diff
using UnityEngine;

namespace TPSRoguelite.InGame.Camera 
{
    public class CameraController : MonoBehaviour 
    {namespace TPSRoguelite.InGame.Camera 
{
    public class CameraController : MonoBehaviour 
    {
-       /// <summary>
-       /// マウス感度
-       /// </summary>
-       private float LOOK_SENSITIVITY = 0.2f;
-
-       /// <summary>
-       /// プレイヤーからの距離
-       /// </summary>
-       private float DISTANCE = 5.0f;
-
-       /// <summary>
-       /// プレイヤーからの高さ
-       /// </summary>
-       private float HEIGHT_OFFSET = 1.5f;
-
-       /// <summary>
-       /// 縦の最小角度
-       /// </summary>
-       private float MIN_PITCH = -10f;
-
-       /// <summary>
-       /// 縦の最大角度
-       /// </summary>
-       private float MAX_PITCH = 60f;
-
        /// <summary>
        /// 追従するターゲット
        /// </summary>
        [SerializeField] private Transform target;

+       [Header("カメラの基本設定")]
+
+       /// <summary>
+       /// マウス感度
+       /// </summary>
+       [SerializeField] private float lookSensitivity = 0.2f;
+        
+       /// <summary>
+       /// 縦の最小角度
+       /// </summary>
+       [SerializeField] private float minPitch = -10f;
+
+       /// <summary>
+       /// 縦の最大角度
+       /// </summary>
+       [SerializeField] private float maxPitch = 60f;
+
+       /// <summary>
+       /// ズーム速度
+       /// </summary>
+       [SerializeField] private float zoomSpeed = 5.0f;
+
+       [Header("カメラの視点")]
+
+       /// <summary>
+       /// 後ろに下がる距離
+       /// </summary>
+       [SerializeField] private float targetDistance = 3.0f;
+
+       /// <summary>
+       /// 高さ
+       /// </summary>
+       [SerializeField] private float targetHeightOffset = 1.2f;
+
+       /// <summary>
+       /// 右にずらす距離
+       /// </summary>
+       [SerializeField] private float targetShoulderOffset = 0.8f;

        /// <summary>
        /// 自動生成されたクラス
        /// </summary>
        private PlayerInputActions inputActions;

        // 既存の変数省略

        /// <summary>
        /// 縦の回転角度（X軸回転）
        /// </summary>
        private float currentPitch = 20f;

+       // 現在のカメラの位置情報（滑らかに変化させるための変数）
+       private float currentDistance;
+       private float currentHeightOffset;
+       private float currentShoulderOffset;

        private void Awake() 
        {
            inputActions = new PlayerInputActions();

            // マウスカーソルを画面中央にロックして非表示にする
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

+           // 最初は通常時の視点をセットしておく
+           currentDistance = targetDistance;
+           currentHeightOffset = targetHeightOffset;
+           currentShoulderOffset = targetShoulderOffset;
        }

        private void OnEnable() 
        {
            inputActions.Enable();    
        }

        private void OnDisable() 
        {
            inputActions.Disable();
        }

        private void Update() 
        {
            // マウスの移動量を取得
            lookInput = inputActions.Player.Look.ReadValue<Vector2>();

            // 感度を掛けて現在の角度に足し引きする
-           currentYaw += lookInput.x * LOOK_SENSITIVITY;
-           currentPitch -= lookInput.y * LOOK_SENSITIVITY;
+           currentYaw += lookInput.x * lookSensitivity;
+           currentPitch -= lookInput.y * lookSensitivity;

-           currentPitch = Mathf.Clamp(currentPitch, MIN_PITCH, MAX_PITCH);
+           currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        private void LateUpdate()
        {
            // カメラの移動は、プレイヤーの移動が終わった後に行う

            // ターゲットが設定されてない場合はエラー回避
            if (target == null) 
            {
                return;
            }

-           // 注視点の計算（プレイヤーの腰あたり）
-           Vector3 targetPosition = target.position + Vector3.up * HEIGHT_OFFSET;

-           // 角度をQuaternionに変換
+           // 1. 現在の数値を、目標の数値に向かって滑らかに変化させる（Mathf.Lerp）
+           currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * zoomSpeed);
+           currentHeightOffset = Mathf.Lerp(currentHeightOffset, targetHeight, Time.deltaTime * zoomSpeed);
+           currentShoulderOffset = Mathf.Lerp(currentShoulderOffset, targetShoulder, Time.deltaTime * zoomSpeed);
+
+           // 2. カメラの回転を計算
            Quaternion rotate = Quaternion.Euler(currentPitch, currentYaw, 0f);
+
-           // 注視点から、計算した角度から後ろ方向へ距離分だけ離した位置を計算
-           Vector3 cameraPosition = targetPosition - (rotate * Vector3.forward * DISTANCE);
+           // 3. 注視点の計算（プレイヤーの高さ）
+           Vector3 basePosition = target.position + Vector3.up * currentHeightOffset;
+
+           // 4. 肩越し視点にするため、カメラにとっての「右方向」へずらす
+           Vector3 shoulderPosition = basePosition + (rotate * Vector3.right * currentShoulderOffset);
+
+           // 5. そこから、カメラにとっての「後ろ方向」へ距離分だけ離す
+           Vector3 cameraPosition = shoulderPosition - (rotate * Vector3.forward * currentDistance);
+
-           // カメラの位置と回転を設定
+           // 6. 最終的な位置と回転を適用
            transform.position = cameraPosition;
            transform.rotation = rotate;
        }
    }
}
```

### 1-2. Playerの回転の修正
カメラが肩越しになっても、プレイヤーの体が違う方向を向いていたら変ですよね。
キャラクターの回転目標を「キー入力の方向」から「カメラが向いている方向の水平ベクトル」に変更して、常に前を見据えるように修正します。

**ファイル名： `PlayerController.cs`**
``` diff
// using省略

namespace TPSRoguelite.InGame.Player
{

    public class PlayerController : MonoBehaviour
    {
        // 変数や関数省略

        private void Move()
        {
-           if (rigidbody == null)
+           if (rigidbody == null || mainCameraTransform == null)
            {
                return;
            }

+           // カメラの水平方向の前方を計算 (入力の有無に関わらず常に計算する)
+           Vector3 cameraForward = mainCameraTransform.forward;
+           cameraForward.y = 0f;
+           cameraForward.Normalize();
+
+           // キャラクターを常に「カメラの向いている方向」へ振り向かせる
+           if (cameraForward != Vector3.zero)
+           {
+               Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
+               rigidbody.rotation = Quaternion.Slerp(rigidbody.rotation, targetRotation, ROTATE_SPEED * Time.fixedDeltaTime);
+           }

            // 入力がない場合はピタッと止める
            if (moveInput == Vector2.zero)
            {
                rigidbody.linearVelocity = new Vector3(0f, rigidbody.linearVelocity.y, 0f);
                CurrentVelocity = Vector3.zero;
                return;
            }

            // カメラ基準の移動方向を計算
-           Vector3 cameraForward = mainCameraTransform.forward;
            Vector3 cameraRight = mainCameraTransform.right;

-           cameraForward.y = 0f;
            cameraRight.y = 0f;
-           cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

-           // キャラクターを進行方向へ滑らかに振り向かせる
-           Quaternion targeRotation = Quaternion.LookRotation(moveDirection);
-           rigidbody.rotation = Quaternion.Slerp(rigidbody.rotation, targeRotation, ROTATE_SPEED * Time.fixedDeltaTime);

+           // 物理演算で移動させる            
            Vector3 targetVelocity = moveDirection * MOVE_SPEED;
            rigidbody.linearVelocity = new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.z);

            // 外部（アニメーションやUIなど）に現在の速度を教えるためにプロパティを更新
            CurrentVelocity = rigidbody.linearVelocity;
        }
    }
}
```
### 動作確認
・プレイボタンを押し、カメラがキャラクターの肩越し（少し右など）に配置されているか確認する。<br>
・マウスを動かした時、プレイヤーの体もちゃんとカメラの方向へ回転していれば大成功！

## 2. 撃つ快感を作ろう（射撃エフェクト）
銃を撃った感触（フィードバック）を作るために、銃口の火花「マズルフラッシュ」を実装します。これがあるだけで、撃っている実感が100倍になります！

### 2-1. マズルフラッシュの実装
弾が発射された関数の中で、マズルフラッシュのパーティクルを再生（Play）する処理を追加します。

**ファイル名： `PlayerController.cs`**
``` diff
// using省略

namespace TPSRoguelite.InGame.Player {

    public class PlayerController : MonoBehaviour
    {
        // 変数省略

        /// <summary>
        /// 武器のID（デフォルトは1）
        /// </summary>
        [SerializeField] private ulong weaponId = 1;

+       /// <summary>
+       /// マズルフラッシュ（銃口の火花）のエフェクト
+       /// </summary>
+       [SerializeField] private ParticleSystem muzzleFlash;

        /// <summary>
        /// 武器のデータ
        /// </summary>
        private WeaponDataRecord currentWeapon;

        // 変数や関数を省略

        private async UniTaskVoid ShootBurstAsync (CancellationToken token)
        {
            canShoot = false;
            for (int i = 0; i < 3; i++)
            {
                if (CurrentAmmo <= 0)
                {
                    canShoot = true;
                    return;
                }

                CurrentAmmo--;
                Shoot();
                Debug.Log($"バースト！ 残弾: {CurrentAmmo}");

                await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: token);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInterval), cancellationToken: token);
            canShoot = true;
        }

        private void Shoot()
        {
+           if (muzzleFlash != null)
+           {
+               muzzleFlash.Play();
+           }

            Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);

            // 光線に何かが当たったか判定
            if (Physics.Raycast(ray, out RaycastHit hitInfo, ATTACK_RANGE))
            {
                Debug.Log($"{hitInfo.collider.name}に命中！");

                // 当たった相手が IDamageable を持っているか確認
                IDamageable target = hitInfo.collider.GetComponent<IDamageable>();

                // ダメージを受ける性質を持ったオブジェクトであればダメージを与える
                if (target != null)
                {
                    target.TakeDamage(currentWeapon.AttackPower);
                }
            }
        }

        private void OnReload(InputAction.CallbackContext context)
        {
            if (isReloading || CurrentAmmo == currentWeapon.MaxAmmo)
            {
                return;
            }

            ReloadAsync().Forget();
        }

        // 関数省略
    }
}
```

### 2-2. マズルフラッシュ（Particle System）の配置
スクリプトが書けたら、実際に光るエフェクトをプレイヤーにくっつけましょう。
1. 講師フォルダから `` というアセットがあるので、それを自分の好きなフォルダにコピペしてください。
2. Projectウィンドウにあるそのアセットを、Hierarchyの `Player`（または武器の先端）にドラッグ＆ドロップして子オブジェクトにする。
3. Hierarchyの `Player` を選択し、`PlayerController` コンポーネントにある `Muzzle Flash` の枠に、今配置したエフェクトをセットする。
4. プレイして、銃を撃ったときに身体越しにピカッ！と光っていればOK！

## 3. 当てる快感を作ろう（ヒットリアクション）
弾が当たったのかどうか分からないと、撃っていても面白くありません。<br>
敵がダメージを受けた際、**「赤く点滅して、一瞬後ろに弾き飛ぶ（ノックバックする）」** アクションゲームの王道の気持ちよさを実装しましょう！

### 2-1. 赤点滅の実装
まずはダメージを受けたときに敵を赤く光らせます。見た目の変化は `EnemyState` で制御を行います。

**ファイル名： `EnemyState.cs`**
``` diff
using UnityEngine.Events;
using Core.Interface;
using Core.MasterData;
+using System;
+using System.Threading;
+using Cysharp.Threading.Tasks;

namespace TPSRoguelite.InGame.Enemy 
{
    public class EnemyState : MonoBehaviour, IDamageable
    {
+       /// <summary>
+       /// 点滅する時間
+       /// </summary>
+       private const float FLASH_DURARION = 0.1f;
+
+       /// <summary>
+       /// キャラクターのレンダラー
+       /// </summary>
+       [SerializeField] private Renderer[] modelRenderers;

+       /// <summary>
+       /// キャラクターの元々の色
+       /// </summary>
+       private Color[] defaultColors;

+       /// <summary>
+       /// 点滅するアニメーションのキャンセルトークン
+       /// </summary>
+       private CancellationTokenSource flashCts;

        public EnemyDataRecord EnemyDataAsset { get; private set; }

        public int CurrentHP { get; private set; }

        public event UnityAction<EnemyState> OnReturnToPoolAction;
        
+       /// <summary>
+       /// ダメージを受けたときに受け取るイベント
+       /// </summary>
+       public event UnityAction OnDamageAction;

        public void Initialize(ulong id)
        {
            EnemyDataAsset = MasterDataAccessor.Instance.GetById<EnemyDataRecord>(id);

+           if (modelRenderers != null)
+           {
+               defaultColors = new Color[modelRenderers.Length];
+               for (int i = 0; i < modelRenderers.Length; i++)
+               {
+                   if (modelRenderers[i] != null)
+                   {
+                       defaultColors[i] = modelRenderers[i].material.color;
+                   }
+               }
+           }
        }

        public void Setup()
        {
            if (EnemyDataAsset == null) 
            {
                Debug.LogError("EnemyDataがセットされていません。");
                return;
            }

            CurrentHP = EnemyDataAsset.MaxHp;
            gameObject.SetActive(true);
+           ResetColor();
        }

        public void TakeDamage(int damageAmount) 
        {
            // マイナスのダメージ（回復）を防ぐ
            if (damageAmount <= 0)
            {
                return;
            }

            CurrentHP -= damageAmount;
            Debug.Log($"{EnemyDataAsset.EnemyName}に{damageAmount}のダメージ！残りHP:{CurrentHP}");
            CurrentHP -= damageAmount;
            Debug.Log($"{EnemyDataAsset.EnemyName}に{damageAmount}のダメージ！残りHP:{CurrentHP}");

-           if (CurrentHP <= 0)
+           if (CurrentHP > 0)
+           {
+               OnDamageAction?.Invoke();

+               flashCts?.Cancel();
+               flashCts?.Dispose();
+               flashCts = null;
+               flashCts = new CancellationTokenSource();
+               var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(flashCts.Token, this.GetCancellationTokenOnDestroy());

+               DamageFlashAsync(linkedCts.Token).Forget();
+           }
+           else
            {
                Die();
            }
        }

        private void Die() 
        {
            Debug.Log($"{EnemyDataAsset.EnemyName}を倒しました");
            gameObject.SetActive(false);
            OnReturnToPoolAction?.Invoke(this);
        }

+       /// <summary>
+       /// 点滅アニメーション
+       /// </summary>
+       private async UniTaskVoid DamageFlashAsync(CancellationToken token)
+       {
+           if (modelRenderers == null)
+           {
+               return;
+           }
+
+           foreach (var renderer in modelRenderers)
+           {
+               if (renderer != null)
+               {
+                   renderer.material.color = Color.red;
+               }
+
+               bool isCanceled = await UniTask.Delay(System.TimeSpan.FromSeconds(FLASH_DURARION), cancellationToken: token).SuppressCancellationThrow();
+
+               if (!isCanceled)
+               {
+                   ResetColor();
+               }
+           }
+       }
+
+       /// <summary>
+       /// 色をリセット
+       /// </summary>
+       private void ResetColor()
+       {
+           if (modelRenderers == null || defaultColors == null)
+           {
+               return;
+           }
+
+           for (int i = 0; i < modelRenderers.Length; i++)
+           {
+               if (modelRenderers[i] != null)
+               {
+                   modelRenderers[i].material.color = defaultColors[i];
+               }
+           }
+       }
    }
}
```

### 3-2. ノックバック
次に、敵がダメージを受けた衝撃で「後ろにズサッ！」と下がる動き（ノックバック）を作ります。移動を管理している `EnemyController` に追記します。

**ファイル名： `EnemyController.cs`**
``` diff
using UnityEngine;
using UnityEngine.AI;
+using Cysharp.Threading.Tasks;
+using System.Threading;
namespace TPSRoguelite.InGame.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        private const string PLAYER_TAG_NAME = "Player";

+       /// <summary>
+       /// ノックバックの強さ
+       /// </summary>
+       private const float KNOCKBACK_FORCE = 2.0f;

+       /// <summary>
+       /// ノックバックの長さ
+       /// </summary>
+       private const float KNOCKBACK_DURATION = 0.15f;

        [SerializeField] private NavMeshAgent navMeshAgent = null;
        [SerializeField] private EnemyState enemyState = null;

        private Transform targetPlayer = null;

+       /// <summary>
+       /// ノックバック動作のキャンセルトークン
+       /// </summary>
+       private CancellationTokenSource hitCts;

        // 関数省略

        private void Update()
        {
            // ターゲット（プレイヤー）とナビが存在しているか
            if (targetPlayer != null && navMeshAgent != null) 
            {
                // プレイヤーの現在位置を毎フレーム目的地として設定する
                navMeshAgent.SetDestination(targetPlayer.position);
            }
        }

+       private void OnEnable ()
+       {
+           enemyState.OnDamageAction -= HandleDamage;
+           enemyState.OnDamageAction += HandleDamage;
+       }

+       private void OnDisable ()
+       {
+           if (enemyState != null)
+           {
+               enemyState.OnDamageAction -= HandleDamage;
+           }

+           if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
+           {
+               navMeshAgent.isStopped = false;
+           }
+       }

+       /// <summary>
+       /// EnemyStateからダメージのイベントが呼ばれた時の処理
+       /// </summary>
+       private void HandleDamage()
+       {
+           hitCts?.Cancel();
+           hitCts?.Dispose();
+           hitCts = null;
+           hitCts = new CancellationTokenSource();
+           var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(hitCts.Token, this.GetCancellationTokenOnDestroy());
+           KnockbackAsync(linkedCts.Token).Forget();
+       }
+
+       /// <summary>
+       /// ノックバック
+       /// </summary>
+       private async UniTaskVoid KnockbackAsync(CancellationToken token)
+       {
+           if (navMeshAgent == null)
+           {
+               return;
+           }
+
+           // 追跡を一時停止する
+           bool wasStopped = navMeshAgent.isStopped;
+           navMeshAgent.isStopped = true;
+
+           // プレイヤーの逆方向（後ろ）に座標をずらす
+           if (targetPlayer != null)
+           {
+               Vector3 dir = (transform.position - targetPlayer.position).normalized;
+
+               // 上下には飛ばさない
+               dir.y = 0;
+               transform.position += dir * KNOCKBACK_FORCE;
+           }
+
+           // 少し待つ（この間は敵が硬直している）
+           bool isCanceled = await UniTask.Delay(System.TimeSpan.FromSeconds(KNOCKBACK_DURATION)).SuppressCancellationThrow();
+
+           // 元に戻して追跡再開
+           if (!isCanceled && navMeshAgent.isActiveAndEnabled)
+           {
+               navMeshAgent.isStopped = wasStopped;
+           }
+       }
    }
}
```
### 動作確認
・プレイして敵を撃ってみる。<br>
・弾が当たった瞬間、敵が一瞬赤くなり、少し後ろに押し出されれば大成功！

## 4. プレイヤーへの配慮（武器UIの可視化）
今「何の武器を持っているか」「残りの弾はいくつか」が分からないと、プレイヤーは不安になります。<br>
武器のモード（SEMI、AUTOなど）によって文字色が変わる、親切でカッコいいUIを作りましょう！

### 4-1.UI（Text）の配置
**【重要：TextMeshPro（TMP）の初期設定】**<br>
もしTextMeshProを使うのが初めてで文字が出ない場合は、上部メニューの `Window > TextMeshPro > Import TMP Essential Resources` をクリックし、右下の `Import` を押してください。
1. Hierarchyの `Canvas` を右クリック ＞ `UI ＞ Text - TextMeshPro` を2つ作る。
2. 1つの名前を `AmmoText`、もう1つを `FireModeText` に変更する。
4. 画面の左下（または見やすい位置）に配置する。

### 4-2.PlayerControllerの改修
UIを操作するため、冒頭に using TMPro; を追加し、表示を更新するプログラムを書きます。<br>
**ファイル名： `PlayerController.cs`** 
``` diff
using Core.Interface;
using Core.MasterData;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TPSRoguelite.InGame.Enums;
using UnityEngine;
using UnityEngine.InputSystem;
+using TMPro;

namespace TPSRoguelite.InGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        // 変数省略

        /// <summary>
        /// マズルフラッシュ（銃口の火花）のエフェクト
        /// </summary>
        [SerializeField] private ParticleSystem muzzleFlash;

+       [Header("Weapon UI")]
+
+       /// <summary>
+       /// 武器タイプを表示するテキスト
+       /// </summary>
+       [SerializeField] private TextMeshProUGUI fireModeText;
+
+       /// <summary>
+       /// 弾数を表示するテキスト
+       /// </summary>
+       [SerializeField] private TextMeshProUGUI ammoText;

        /// <summary>
        /// 武器のデータ
        /// </summary>
        private WeaponDataRecord currentWeapon;

        // 変数省略

        private void Start ()
        {
            gameObject.SetActive(false);
        }

        public void Setup ()
        {
            currentWeapon = MasterDataAccessor.Instance.GetById<WeaponDataRecord>(1);
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

+           UpdateWeaponUI();

            gameObject.SetActive(true);
        }

        private void OnEnable ()
        {
            if (inputActions != null)
            {
                inputActions.Enable();
            }
        }

        // 関数省略

        private async UniTaskVoid ShootSemiAutoAsync (CancellationToken token)
        {
            canShoot = false;

            if (CurrentAmmo <= 0)
            {
                ReloadAsync().Forget();
                return;
            }

            CurrentAmmo--;
+           UpdateCurrentAmmoUI();
            Debug.Log($"バン！ 残弾: {CurrentAmmo}");
            Shoot();

            await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInterval), cancellationToken: token);

            canShoot = true;
        }

        private async UniTaskVoid ShootFullAutoAsync (CancellationToken token)
        {
            canShoot = false;

            while (!token.IsCancellationRequested)
            {
                if (CurrentAmmo <= 0)
                {
                    ReloadAsync().Forget();
                    break;
                }

                CurrentAmmo--;
+               UpdateCurrentAmmoUI();
                Debug.Log($"フルオート発射！ 残弾: {CurrentAmmo}");
                Shoot();

                bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: token).SuppressCancellationThrow();

                if (isCanceled)
                {
                    break;
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInterval), cancellationToken: this.GetCancellationTokenOnDestroy());

            canShoot = true;
        }

        private async UniTaskVoid ShootBurstAsync (CancellationToken token)
        {
            canShoot = false;
            for (int i = 0; i < 3; i++)
            {
                if (CurrentAmmo <= 0)
                {
                    canShoot = true;
                    return;
                }

                CurrentAmmo--;
+               UpdateCurrentAmmoUI();
                Shoot();
                Debug.Log($"バースト！ 残弾: {CurrentAmmo}");

                await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: token);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInterval), cancellationToken: token);
            canShoot = true;
        }

        private void Shoot ()
        {
            // 処理省略
        }

        private void OnReload (InputAction.CallbackContext context)
        {
            if (isReloading || CurrentAmmo == currentWeapon.MaxAmmo)
            {
                return;
            }

            ReloadAsync().Forget();
        }

        private async UniTask ReloadAsync ()
        {
            isReloading = true;
            Debug.Log("リロード中");

            await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.ReloadTime), cancellationToken: this.GetCancellationTokenOnDestroy());

            CurrentAmmo = currentWeapon.MaxAmmo;
+           UpdateCurrentAmmoUI();
            isReloading = false;
            Debug.Log("リロード完了");
        }

        private void DrawLaserPointer ()
        {
            // 処理省略
        }

+       /// <summary>
+       /// 武器タイプの表示を更新
+       /// </summary>
+       private void UpdateWeaponUI()
+       {
+           if (fireModeText == null || ammoText == null)
+           {
+               return;
+           }
+
+           FireType fireType = (FireType)currentWeapon.FireType;
+           switch (fireType)
+           {
+               case FireType.SemiAuto:
+                   fireModeText.text = "Semi-Auto";
+                   fireModeText.color = Color.white;
+                   break;
+               case FireType.Burst:
+                   fireModeText.text = "Burst";
+                   fireModeText.color = Color.yellow;
+                   break;
+               case FireType.FullAuto:
+                   fireModeText.text = "Full-Auto";
+                   fireModeText.color = Color.red;
+                   break;
+               default:
+                   fireModeText.text = "Unknown";
+                   break;
+           }
+
+           UpdateCurrentAmmoUI();
+       }
+
+       /// <summary>
+       /// 弾薬表示の更新
+       /// </summary>
+       private void UpdateCurrentAmmoUI()
+       {
+           if (ammoText != null)
+           {
+               ammoText.SetText($"{CurrentAmmo}/{currentWeapon.MaxAmmo}");
+           }
+       }
    }
}
```

### 4-3. Playerプレハブにテキストを割り当て
1. Hierarchyから `Player` を選択する。
2. `PlayerController` コンポーネントの `Ammo Text` と `Fire Mode Text` の枠に、先ほど作成したUIをそれぞれドラッグ＆ドロップで割り当てる。
3. プレイして、装備した銃のタイプ（SEMIなど）が表示され、撃つ度に弾数が減っていれば完成です！

## 5. リロード時間の可視化
リロード中、「あと何秒で撃てるか」が分かるサークル画像と、「リロード中…」というテキストを作ります。<br>
ここでは、アニメーションツール 「DOTween（ドゥートゥイーン）」 を導入し、面倒な計算をせずに「たった1行」でUIをアニメーションさせましょう！

### 5-1. サークルUIの作成
**【重要：2D Spriteパッケージの準備】** もし右クリックメニューに「Sprites > Circle」が見当たらない場合は、以下の手順で追加します。
1. 上部メニューの `Window > Package Manager` を開きます。
2. 左上を `Packages: Unity Registry` に変更し、右上の検索窓で `2D Sprite` を検索します。
3. `2D Sprite` を選んで右下の `Install` を押します。

**UIの階層（親子関係）を作って配置する** リロード中はサークルの下にテキストも一緒に出すため、全体をまとめる「親オブジェクト」を作ります。これを作っておくと、表示・非表示のプログラムがとても簡単になります。
1. Projectウィンドウの `MyProject` に `Sprites` というフォルダを作成します。
2. **今作ったSpritesフォルダ内で右クリック** ＞ `Create > 2D > Sprites > Circle` で真っ白な円画像を作ります。
3. HierarchyのCanvasの中で右クリック ＞ `Create Empty` を選び、空のオブジェクトを作って名前を `ReloadUI` にします。（これが全体をまとめる親玉になります。画面の中央に配置してください）
4. **今作った `ReloadUI` の上で右クリック** ＞ `UI > Image` を作成し、名前を `ReloadCircleImage` にします。
5. `ReloadCircleImage` の `Source Image` に白円をセットし、`Image Type` を `Filled、Fill Method` を `Radial 360` にします。（`Fill Amount` を動かすと時計のように欠けるのがわかります）
6. もう一度 `ReloadUI` の上で右クリック ＞ `UI > Text - TextMeshPro` を作成します。テキストの中身を **「リロード中…」** に書き換え、サークルの下になるように配置します。

### 5-2. DOTweenのインストールと初期設定
1. Asset Storeから `DOTween (HOTween v2)` をインポートします。
2. インポートが終わると緑色のウィンドウが開きます。もし開かない場合は、上部メニューの `Tools > Demigiant > DOTween Utility Panel` を開いてください。
3. そのウィンドウの中にある `Setup DOTween...` という緑色のボタンを押して、そのまま `Apply` を押します。これをやらないとエラーになります！

### 5-3. 魔法の1行でアニメーションさせる！
PlayerController にリロードUIの枠を追加し、リロード処理を書き換えます。
**ファイル名： `PlayerController.cs`**
``` diff
using Core.Interface;
using Core.MasterData;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using TPSRoguelite.InGame.Enums;
using UnityEngine;
using UnityEngine.InputSystem;
+using UnityEngine.UI;
+using DG.Tweening;

namespace TPSRoguelite.InGame.Player
{

    public class PlayerController : MonoBehaviour
    {
        // 変数省略

        [Header("Weapon UI")]
        [SerializeField] private TextMeshProUGUI fireModeText;
        [SerializeField] private TextMeshProUGUI ammoText;

+       [Header("Reload UI")]
+
+       /// <summary>
+       /// リロード中のテキストと画像をまとめたオブジェクト
+       /// </summary>
+       [SerializeField] private GameObject reloadUI;
+
+       /// <summary>
+       /// リロード中、「あと何秒で撃てるか」が分かるサークル画像
+       /// </summary>
+       [SerializeField] private Image reloadCircleImage;

        private WeaponDataRecord currentWeapon;

        // 変数省略

        private void Start ()
        {
            gameObject.SetActive(false);
        }

        public void Setup ()
        {
            currentWeapon = MasterDataAccessor.Instance.GetById<WeaponDataRecord>(1);
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

            UpdateWeaponUI();

+           if (reloadUI != null)
+           {
+               reloadUI.SetActive(false);
+           }

            gameObject.SetActive(true);
        }

        private void OnEnable ()
        {
            if (inputActions != null)
            {
                inputActions.Enable();
            }
        }

        // 関数省略

        /// <summary>
        /// セミオートの射撃処理
        /// </summary>
        private async UniTaskVoid ShootSemiAutoAsync(CancellationToken token) 
        {
            if (CurrentAmmo == 0) 
            {
-               ReloadAsync().Forget();                
+               Reload();
                return;
            }

            canShoot = false;

            CurrentAmmo--;
            UpdateCurrentAmmoUI();
            Debug.Log($"セミオートで撃った！弾数：{CurrentAmmo}");
            Shoot();

            await UniTask.Delay(System.TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: token);

            canShoot = true;
        }

        /// <summary>
        /// バーストの射撃処理
        /// </summary>
        private async UniTaskVoid ShootBurstAsync(CancellationToken token) 
        {
            canShoot = false;

            for (int i = 0; i < 3; i++) 
            {
                if (CurrentAmmo <= 0)
                {
-                   ReloadAsync().Forget();                    
+                   Reload();
                    break;
                }

                CurrentAmmo--;
                UpdateCurrentAmmoUI();
                Shoot();
                Debug.Log($"バースト！残弾数：{CurrentAmmo}");

                await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInteval), cancellationToken: token);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: token);
            canShoot = true;
        }

        private async UniTaskVoid ShootFullAutoAsync(CancellationToken token)
        {
            canShoot = false;

            while (!token.IsCancellationRequested) 
            {
                if (CurrentAmmo <= 0) 
                {
-                   ReloadAsync().Forget();
+                   Reload();
                    break;
                }

                CurrentAmmo--;
                UpdateCurrentAmmoUI();
                Debug.Log($"フルオート！残弾数：{CurrentAmmo}");
                Shoot();

                bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInteval), cancellationToken: token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    break;
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: this.GetCancellationTokenOnDestroy());

            canShoot = true;
        }

        /// <summary>
        /// 共通の射撃処理
        /// </summary>
        private void Shoot() 
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);

            // 光線に何かが当たったか判定
            if (Physics.Raycast(ray, out RaycastHit hitInfo, ATTACK_RANGE)) {
                Debug.Log($"{hitInfo.collider.name}に命中！");

                // 当たった相手が IDamageable を持っているか確認
                IDamageable target = hitInfo.collider.GetComponent<IDamageable>();

                // ダメージを受ける性質を持ったオブジェクトであればダメージを与える
                if (target != null) {
                    target.TakeDamage(currentWeapon.AttackPower);
                }
            }
        }

        private void OnReload(InputAction.CallbackContext context)
        {
            if (isReloading || CurrentAmmo == currentWeapon.MaxAmmo) {
                return;
            }

-           ReloadAsync().Forget();
+           Reload();
        }

-       private async UniTask ReloadAsync()
+       private void Reload()
        {
            isReloading = true;
-           Debug.Log("リロード中");
-
-           await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.ReloadTime), cancellationToken: this.GetCancellationTokenOnDestroy());
-
-           CurrentAmmo = currentWeapon.MaxAmmo;
-           UpdateCurrentAmmoUI();
-           isReloading = false;
-           Debug.Log("リロード完了");

+           if (reloadUI != null)
+           {
+               reloadUI.gameObject.SetActive(true);
+           }
+
+           if (reloadCircleImage != null)
+           {
+               reloadCircleImage.fillAmount = 0f;
+           }
+
+           DOVirtual.Float(0f, 1f, currentWeapon.ReloadTime, UpdateReloadUI).SetEase(Ease.Linear).OnComplete(FinishReload);
        }

        /// <summary>
        /// レーザーポインターの描画
        /// </summary>
        private void DrawLaserPointer()
        {
            if (laserLineRenderer == null || weponOrigin == null || mainCameraTransform == null) 
            {
                return;
            }

            laserLineRenderer.SetPosition(0, weponOrigin.position);

            Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, LASER_MAX_DISTANCE))
            {
                laserLineRenderer.SetPosition(1, hitInfo.point);
            }
            else
            {
                laserLineRenderer.SetPosition(1, ray.GetPoint(LASER_MAX_DISTANCE));
            }
        }

        private void UpdateFireModeUI ()
        {
            if (fireModeText == null || currentWeapon == null)
            {
                return;
            }

            FireType fireType = (FireType)currentWeapon.WeaponFireType;
            switch (fireType)
            {
                case FireType.SemiAuto:
                    fireModeText.text = "Semi-Auto";
                    fireModeText.color = Color.white;
                    break;
                case FireType.Burst:
                    fireModeText.text = "Burst";
                    fireModeText.color = Color.yellow;
                    break;
                case FireType.FullAuto:
                    fireModeText.text = "Full-Auto";
                    fireModeText.color = Color.red;
                    break;
                default:
                    fireModeText.text = "Unknown";
                    break;
            }

            UpdateCurrentAmmoUI();
        }

        private void UpdateCurrentAmmoUI()
        {
            if (ammoText != null && currentWeapon != null)
            {
                ammoText.text = $"{CurrentAmmo}/{currentWeapon.MaxAmmo}";
            }
        }

+       /// <summary>
+       /// リロードUIの更新
+       /// </summary>
+       private void UpdateReloadUI(float value)
+       {
+           if (reloadCircleImage != null)
+           {
+               reloadCircleImage.fillAmount = value;
+           }
+       }
+
+       /// <summary>
+       /// リロード終了処理
+       /// </summary>
+       private void FinishReload()
+       {
+           if (reloadUI != null)
+           {
+               reloadUI.SetActive(false);
+           }
+
+           CurrentAmmo = currentWeapon.MaxAmmo;
+           UpdateCurrentAmmoUI();
+           isReloading = false;
+       }
    }
}
```

**お疲れ様でした！ カメラ、エフェクト、リアクション、そしてUI。今日のアップデートで、あなたのゲームの手触りは劇的に良くなりました！**
