# 第10週：吸い込む快感とレベルアップ！「成長」の手触りを極める

## 本日の目標
今日は、ゲームの醍醐味である「成長」のシステムを作り、ローグライクの面白さに近づけたいと思います！ さらに、先生が用意した「魔法のシェーダー」を使って、画面の演出をプロレベルに強化します。
1. 吸い込む快感：近づくと「シュバッ！」と飛んでくる経験値オーブを作る。
2. 成長のロジック：経験値ゲージとレベルアップのシステムを構築する。
3. UIの魔術（グラデーション）：マテリアルの力だけでUIを綺麗なグラデーションに染め上げる。

## 1. 経験値オーブを作ろう（マグネット吸引）
ただ触れるだけではなく、近づくと自動でプレイヤーに吸い寄せられる気持ちいいオーブを作ります。<br>
`Scripts/InGame/Item` フォルダを作成し、`ExperienceOrb.cs` を作ります。

**ファイル名： `ExperienceOrb.cs`**
``` cs
using TPSRoguelite.InGame.Player;
using UnityEngine;

namespace TPSRoguelite.InGame.Item
{
    public class ExperienceOrb : MonoBehaviour
    {
        /// <summary>
        /// プレイヤーに引き寄せられる範囲
        /// </summary>
        private const float MAGNET_RANGE = 5f;

        /// <summary>
        /// プレイヤーに引き寄せられる速度
        /// </summary>
        private const float MOVE_SPEED = 15f;

        /// <summary>
        /// プレイヤーのタグ
        /// </summary>
        private const string PLAYER_TAG = "Player";

        /// <summary>
        /// プレイヤーのTransform
        /// </summary>
        private Transform targetPlayer = null;

        /// <summary>
        /// プレイヤーに引き寄せられているかどうか
        /// </summary>
        private bool isFollowing = false;

        private void Start ()
        {
            // 出現時にプレイヤーのTransformを取得する
            GameObject playerObj = GameObject.FindGameObjectWithTag(PLAYER_TAG);
            if (playerObj != null)
            {
                targetPlayer = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("プレイヤーが見つかりませんでした。");
            }
        }

        private void Update ()
        {
            if (targetPlayer == null)
            {
                return;
            }

            if (isFollowing)
            {
                // プレイヤーに向かって移動する
                transform.position = Vector3.MoveTowards(transform.position, targetPlayer.position, MOVE_SPEED * Time.deltaTime);
            }
            else
            {
                // プレイヤーとの距離を計算し、引き寄せ範囲内であれば引き寄せを開始する
                float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
                if (distanceToPlayer <= MAGNET_RANGE)
                {
                    // プレイヤーに引き寄せられるようになる
                    isFollowing = true;
                }
            }
        }

        /// <summary>
        /// プレイヤーに触れたときの処理（コライダーの Is Trigger がオンになっている必要があります）
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(PLAYER_TAG))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    // プレイヤーに触れたら経験値を付与する処理をここに追加
                }
                else
                {
                    Debug.LogWarning("PlayerController コンポーネントが見つかりませんでした。");
                }

                // 経験値オーブを破棄する
                Destroy(gameObject);
            }
        }
    }
}
```

**エディタでの作業**
1. Hierarchyウィンドウの何もない場所で右クリックし、`3D Object > Sphere（球体）`を作成します。
2. 名前を `ExperienceOrb` に変更し、Inspectorの上部にある `Transform` の `Scale` をすべて `0.3` くらいに小さくします。（色が欲しい場合は黄色いマテリアルなどを作って適用してください）
3. 今作った `ExperienceOrb` に、先ほど書いたスクリプト `ExperienceOrb.cs` をドラッグ＆ドロップでアタッチします。
4. Inspectorにある `Sphere Collider` コンポーネントを探し、`Is Trigger` の左にあるチェックボックスに必ずチェックを入れてください。（入れないとプレイヤーにぶつかって弾き飛ばされてしまいます！）
5. Projectウィンドウに `Prefabs`（または Items）フォルダを用意し、そこへ `ExperienceOrb` をドラッグ＆ドロップして `Prefab（プレハブ）化` します。青い箱のアイコンになったら、Hierarchy上にある元のオーブは `Delete` キーで削除してOKです。

## 2. 敵からのドロップ処理
敵が死んだときに、さっき作ったオーブを落とすようにします。

**ファイル名： `EnemyState.cs`**
``` diff
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TPSRoguelite.InGame.Enemy 
{
    public class EnemyState : MonoBehaviour, IDamageable
    {
        /// <summary>
        /// 点滅する時間
        /// </summary>
        private const float FLASH_DURATION = 0.1f;
+
+       /// <summary>
+       /// オーブのドロップ位置の高さのオフセット
+       /// </summary>
+       private const float ORB_DROP_HEIGHT_OFFSET = 0.5f;

        /// <summary>
        /// キャラクターのレンダラー
        /// </summary>
        [SerializeField] private Renderer[] modelRenderers;
+
+       [Header("ドロップアイテム")]
+
+       /// <summary>
+       /// ドロップするオーブ
+       /// </summary>
+       [SerializeField] private GameObject experienceOrbPrefab;

        /// <summary>
        /// キャラクターの元々の色
        /// </summary>
        private Color[] defaultColors;

        /// <summary>
        /// 点滅するアニメーションのキャンセルトークン
        /// </summary>
        private CancellationTokenSource flashCts;

        /// <summary>
        /// 敵のデータ
        /// </summary>
        public EnemyDataRecord EnemyDataAsset { get; private set; }

        /// <summary>
        /// 現在の体力
        /// </summary>
        public int CurrentHP { get; private set; }

        public event UnityAction<EnemyState> OnReturnToPoolAction;

        /// <summary>
        /// ダメージを受けたときに受け取るイベント
        /// </summary>
        public event UnityAction OnDamageAction;

        public void Initialize(ulong id)
        {
            EnemyDataAsset = MasterDataAccessor.Instance.GetById<EnemyDataRecord>(id);

            if (modelRenderers != null)
            {
                defaultColors = new Color[modelRenderers.Length];
                for (int i = 0; i < modelRenderers.Length; i++)
                {
                    defaultColors[i] = modelRenderers[i].material.color;
                }
            }
        }

        public void Setup()
        {
            if (EnemyDataAsset == null) 
            {
                Debug.LogError("EnemyDataがセットされていません。");
                return;
            }

            CurrentHP = EnemyDataAsset.MaxHP;
            gameObject.SetActive(true);
            ResetColor();
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

            if (CurrentHP > 0)
            {
                OnDamageAction?.Invoke();

                flashCts?.Cancel();
                flashCts?.Dispose();
                flashCts = new CancellationTokenSource();
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(flashCts.Token, this.GetCancellationTokenOnDestroy());

                DamageFlashAsync(linkedCts.Token).Forget();
            }
            else
            {
                Die();
            }
        }

        private void Die() 
        {
            Debug.Log($"{EnemyDataAsset.EnemyName}を倒しました");
+
+           // 経験値オーブをドロップする
+           if (experienceOrbPrefab != null)
+           {
+               Vector3 spawnPosition = transform.position + Vector3.up * ORB_DROP_HEIGHT_OFFSET;
+               Instantiate(experienceOrbPrefab, spawnPosition, Quaternion.identity);
+           }

            gameObject.SetActive(false);
            OnReturnToPoolAction?.Invoke(this);
        }

        /// <summary>
        /// ダメージを受けたときの点滅処理
        /// </summary>
        private async UniTaskVoid DamageFlashAsync(CancellationToken token)
        {
            if (modelRenderers == null || defaultColors == null)
            {
                return;
            }

            foreach (var renderer in modelRenderers)
            {
                if (renderer != null)
                {
                    renderer.material.color = Color.red;
                }

                bool isCancelled = await UniTask.Delay(TimeSpan.FromSeconds(FLASH_DURATION), cancellationToken: token).SuppressCancellationThrow();

                if (!isCancelled)
                {
                    ResetColor();
                }
            }
        }
    }
}
```
**エディタでの作業**<br>
Enemyプレハブの `Experience Orb Prefab` 枠に、先ほど作ったオーブのプレハブをセットします。

## 3. プレイヤーの経験値とレベルアップ処理
PlayerController.cs を開き、経験値の受け皿と、レベルアップの仕組みを追加します。

### 3-1. UIの配置
まずは画面に経験値ゲージと文字を作ります。
1. Hierarchyウィンドウにある `Canvas` の上で右クリックし、`UI > Slider` を作成します。名前を `ExpSlider` に変更します。
2. `ExpSlider` を選択し、Inspectorにある `Slider` コンポーネントの `Interactable` のチェックを外します。（外さないと、ゲーム中にマウスでゲージを動かせてしまいます）
3. さらに `Canvas` の上で右クリックし、`UI > Text - TextMeshPro` を作成します。名前を `LevelUpText` に変更します。
4. 画面の真ん中に大きく配置し、Inspectorでテキストの色を黄色などに、文字を「LEVEL UP!」に変更します。

### 3-2. 経験値の取得
**ファイル名： `PlayerController.cs`**
``` diff
using Core.Interface;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TPSRoguelite.InGame.Enum;
using Core.MasterData;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

namespace TPSRoguelite.InGame.Player {

    public class PlayerController : MonoBehaviour
    {
        // 変数省略
        /// <summary>
        /// 現在の弾数
        /// </summary>
        public int CurrentAmmo { get; private set; }
+
+       /// <summary>
+       /// 現在の経験値
+       /// </summary>
+       public int CurrentExp { get; private set; }

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Setup()
        {
            currentWeapon = MasterDataAccessor.Instance.GetById<WeaponDataRecord>(weaponId);

            if (currentWeapon != null)
            {
              CurrentAmmo = currentWeapon.MaxAmmo;
            }
            else
            {
                Debug.LogError("WeaponDataがありません。");
            }

+           CurrentExp = 0;

            inputActions = new PlayerInputActions();
            inputActions.Player.Fire.performed += OnFire; // 押し続けていると呼ばれる
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

            UpdateFireModeUI();

            if (reloadUI != null)
            {
              reloadUI.SetActive(false);
            }

            gameObject.SetActive(true);
        }

        //関数省略

        /// <summary>
        /// リロード完了時の処理
        /// </summary>
        private void FinishReload()
        {
            if (reloadUI != null)
            {
                reloadUI.SetActive(false);
            }

            CurrentAmmo = currentWeapon.MaxAmmo;
            UpdateCurrentAmmoUI();
            isReloading = false;
        }
+
+       /// <summary>
+       /// 経験値を追加する
+       /// </summary>
+       public void AddExperience(int amount)
+       {
+           CurrentExp += amount;
+           Debug.Log($"経験値を{amount}獲得！現在の経験値: {CurrentExp}");
+       }
    }
}
```

ExperienceOrb.csに経験値を追加する処理を実装します。

**ファイル名： `ExperienceOrb.cs`**
``` diff
using TPSRoguelite.InGame.Player;
using UnityEngine;

namespace TPSRoguelite.InGame.Item
{
    public class ExperienceOrb : MonoBehaviour
    {
        /// <summary>
        /// プレイヤーに引き寄せられる範囲
        /// </summary>
        private const float MAGNET_RANGE = 5f;

        /// <summary>
        /// プレイヤーに引き寄せられる速度
        /// </summary>
        private const float MOVE_SPEED = 15f;

        /// <summary>
        /// プレイヤーのタグ
        /// </summary>
        private const string PLAYER_TAG = "Player";

        /// <summary>
        /// プレイヤーのTransform
        /// </summary>
        private Transform targetPlayer = null;

        /// <summary>
        /// プレイヤーに引き寄せられているかどうか
        /// </summary>
        private bool isFollowing = false;

        private void Start ()
        {
            // 出現時にプレイヤーのTransformを取得する
            GameObject playerObj = GameObject.FindGameObjectWithTag(PLAYER_TAG);
            if (playerObj != null)
            {
                targetPlayer = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("プレイヤーが見つかりませんでした。");
            }
        }

        private void Update ()
        {
            if (targetPlayer == null)
            {
                return;
            }

            if (isFollowing)
            {
                // プレイヤーに向かって移動する
                transform.position = Vector3.MoveTowards(transform.position, targetPlayer.position, MOVE_SPEED * Time.deltaTime);
            }
            else
            {
                // プレイヤーとの距離を計算し、引き寄せ範囲内であれば引き寄せを開始する
                float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
                if (distanceToPlayer <= MAGNET_RANGE)
                {
                    // プレイヤーに引き寄せられるようになる
                    isFollowing = true;
                }
            }
        }

        /// <summary>
        /// プレイヤーに触れたときの処理（コライダーの Is Trigger がオンになっている必要があります）
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(PLAYER_TAG))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
-                   // プレイヤーに触れたら経験値を付与する処理をここに追加
+                   player.AddExperience(1);
                }
                else
                {
                    Debug.LogWarning("PlayerController コンポーネントが見つかりませんでした。");
                }

                // 経験値オーブを破棄する
                Destroy(gameObject);
            }
        }
    }
}

```

### 3-3. レベルアップ処理
まずは、レベルアップをしたときにエフェクトを出したいので、そのエフェクトを作成します。
1. 講師フォルダから `LevelUpEffect` というアセットがあるので、それを自分の好きなフォルダにコピペしてください。
2. Projectウィンドウにあるそのアセットを、Hierarchyの `Player`にドラッグ＆ドロップして子オブジェクトにする。

**ファイル名： `PlayerController.cs`**
``` diff
using Core.Interface;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TPSRoguelite.InGame.Enum;
using Core.MasterData;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

namespace TPSRoguelite.InGame.Player {

    public class PlayerController : MonoBehaviour
    {
        // 変数省略

        [Header("Relaod UI")]

        [SerializeField] private GameObject reloadUI;
        [SerializeField] private Image reloadCircleImage;
+
+       [Header("経験値＆レベルアップのUI")]
+
+       /// <summary>
+       /// 経験値を表示するスライダーUI
+       /// </summary>
+       [SerializeField] private Slider expSlider;
+
+       /// <summary>
+       /// レベルアップ時に表示するテキストUI
+       /// </summary>
+       [SerializeField] private TextMeshProUGUI levelUpText;
+
+       /// <summary>
+       /// レベルアップ時のエフェクト
+       /// </summary>
+       [SerializeField] private ParticleSystem levelUpEffect;

        // 変数省略

        /// <summary>
        /// 現在の弾数
        /// </summary>
        public int CurrentAmmo { get; private set; }

        /// <summary>
        /// 現在の経験値
        /// </summary>
        public int CurrentExp { get; private set; }

+       /// <summary>
+       /// 現在のレベル
+       /// </summary>
+       public int CurrentLevel { get; private set; } = 1;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Setup()
        {
            currentWeapon = MasterDataAccessor.Instance.GetById<WeaponDataRecord>(weaponId);

            if (currentWeapon != null)
            {
                CurrentAmmo = currentWeapon.MaxAmmo;
            }
            else
            {
                Debug.LogError("WeaponDataがありません。");
            }

            CurrentExp = 0;
+           CurrentLevel = 1;
+
+           if (levelUpText != null)
+           {
+               // レベルアップ時のテキストを非表示にする
+               levelUpText.enabled = false;
+           }
+
+           UpdateExpUI();

            inputActions = new PlayerInputActions();
            inputActions.Player.Fire.performed += OnFire; // 押し続けていると呼ばれる
            inputActions.Player.Fire.canceled += OnFire;
            inputActions.Player.Reload.performed += OnReload;

            if (UnityEngine.Camera.main != null) {
                mainCameraTransform = UnityEngine.Camera.main.transform;
            } else {
                Debug.LogError("Main Cameraが見つかりません。");
            }

            UpdateFireModeUI();

            if (reloadUI != null)
            {
                reloadUI.SetActive(false);
            }

            gameObject.SetActive(true);
        }

        private void OnEnable() {
            inputActions?.Enable();
        }

        private void OnDisable() {
            inputActions?.Disable();
        }

        private void Update() {
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            DrawLaserPointer();
        }

        private void FixedUpdate() {
            // 物理演算に関わる移動処理になるため、FixedUpdateで行う
            Move();
        }

        /// <summary>
        /// 経験値を追加する
        /// </summary>
        public void AddExperience(int amount)
        {
            CurrentExp += amount;
            Debug.Log($"経験値を{amount}獲得！現在の経験値: {CurrentExp}");

+           // レベルアップ判定
+           if (CurrentExp >= RequiredExp)
+           {
+               LevelUp();
+           }
+
+           // UIゲージの長さを更新
+           UpdateExpUI();
        }

+       /// <summary>
+       /// レベルアップ処理
+       /// </summary>
+       private void LevelUp()
+       {
+           CurrentLevel++;
+
+           // 余った経験値を消さずに、次のレベルに持ち越す
+           CurrentExp -= RequiredExp;
+
+           Debug.Log($"レベルアップ！現在のレベル: {CurrentLevel}, 次のレベルまでの経験値: {RequiredExp - CurrentExp}");
+
+           // レベルアップのエフェクトを再生
+           if (levelUpEffect != null)
+           {
+               levelUpEffect.Play();
+           }
+
+           ShowLevelUpTextAsync().Forget();
+       }
+
+       /// <summary>
+       /// UIゲージの長さを更新する
+       /// </summary>
+       private void UpdateExpUI()
+       {
+           if (expSlider != null)
+           {
+               // 0.0（空） ～ 1.0（満タン） の割合を計算してSliderにセットする
+               expSlider.value = (float)CurrentExp / RequiredExp;
+           }
+       }
+
+       /// <summary>
+       /// レベルアップの文字を表示する非同期処理
+       /// </summary>
+       private async UniTaskVoid ShowLevelUpTextAsync()
+       {
+           if (levelUpText == null)
+           {
+               return;
+           }
+
+           levelUpText.enabled = true;
+           levelUpText.SetText($"Level Up!\n<size=50%>Lv.{CurrentLevel}</size>");
+
+           // 2秒間表示した後に非表示にする
+           await UniTask.Delay(TimeSpan.FromSeconds(LEVEL_UP_EFFECT_DURATION), cancellationToken: this.GetCancellationTokenOnDestroy());
+
+           levelUpText.enabled = false;
+       }
    }
}
```
**エディタでの作業**
1. Hierarchyウィンドウの `Player` をクリックして選択します。
2. Inspectorウィンドウを下へスクロールし、`PlayerController` コンポーネントに追加された `Exp Slider` と `Level Up Text` の枠を見つけます。
3. その2つの枠に、先ほどCanvasの中に作ったUIをそれぞれドラッグ＆ドロップで割り当てます。
4. Projectウィンドウにあるそのアセットを、Projectウィンドウの `Player.prefab` にドラッグ＆ドロップして子オブジェクトにします。
5. Hierarchyの `Player` を選択し、`PlayerController` コンポーネントにある `Muzzle Flash` の枠に、今配置したエフェクトをセットします。

## 4. 演出強化：自作シェーダーで作るグラデーションUI
経験値バーを綺麗なグラデーションにしたいですが、画像素材はありません。 そこで、UI専用の「シェーダー（絵の具）」と「マテリアル（パレット）」を自作して色を塗ります！
### 4-1. シェーダー（Shader）を作成する
シェーダーとは、グラフィックボードに「どうやって色を塗るか」を直接命令する魔法のコードです。C#とは全く違う言葉で書かれています。
1. Projectウィンドウで右クリック ＞ `Create > Shader > Standard Surface Shader` を作成します。
2. 名前を `UIGradient` に変更して開きます。
3. 中に書かれているコードを全て消して、先生が用意した以下のコードをまるごとコピー＆ペーストして保存します。
   (講師フォルダにもコードがあります)
```
Shader "Custom/UIGradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (1,1,1,1)
        _BottomColor ("Bottom Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // UI用の透過や描画順の設定
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _TopColor;
            float4 _BottomColor;
            sampler2D _MainTex;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                // UVのY座標（0.0 〜 1.0）を使って、下と上の色を混ぜ合わせる
                float4 gradColor = lerp(_BottomColor, _TopColor, v.uv.y);
                
                // 元のUIの色にグラデーション色を掛け合わせる
                o.color = v.color * gradColor;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // テクスチャの色と計算した色を最終出力する
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                return col;
            }
            ENDCG
        }
    }
}
```

### 4-2. マテリアル（Material）を作成して適用する
シェーダー（絵の具）ができたら、それを使うためのマテリアル（パレット）を作ります。
1. Projectウィンドウで右クリック ＞ `Create > Material` を作成し、名前を `GradientMaterial` にします。
2. 作成したマテリアルをクリックし、Inspectorの一番上にある `Shader` のプルダウンを開きます。
3. リストの中から `Custom > UIGradient` を選びます。
4. すると、Inspectorに `Top Color` と `Bottom Color` という項目が現れるので、好きな色（黄色とオレンジなど）を設定します。
5. Hierarchyで経験値Sliderの中の `Fill Area > Fill` を選択します。
6. Inspectorの `Image` コンポーネントにある `Material` の枠に、今作った `GradientMaterial` をドラッグ＆ドロップします。
   
これだけで、経験値バーが綺麗なグラデーションに変わります！ プレイボタンを押して、大量のオーブを吸い込む快感とレベルアップの達成感を味わってみましょう！
