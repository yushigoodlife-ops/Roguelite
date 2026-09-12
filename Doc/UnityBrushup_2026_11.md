# 第11週：時を止めて最強を選べ！「ポーズとスキル選択UI」

## 本日の目標
ヴァンサバ系ゲームで一番楽しい瞬間、それは「レベルアップしてゲームが止まり、新しい能力を選ぶ時間」です！<br>
今日は、CSVで管理されたデータからランダムに3つのスキルを選び出し、プレイヤーを強化するシステムを完成させます。
1. **スキルの定義**：CSVで5つのスキルを作り、ゲームに読み込む。
2. **成長の受け皿**：プレイヤーに「バフ（強化状態）」を記憶する変数を作る。
3. **時を操る**：ゲームをピタッと止め、マウスカーソルを解放してActionMap（操作）を切り替える。
4. **ランダム抽選**：選ばれた3つのスキルをUIに表示し、ボタンで取得する。

## 1.スキルのデータ構造を作ろう
まずは、スキルの種類を定義し、CSVのデータを読み込むための「データカセット（ScriptableObject）」を作ります。<br>
`Scripts/InGame/Enums` に `SkillType.cs` を作ります。
**ファイル名： `SkillType.cs`**
``` cs
namespace TPSRoguelite.InGame.Enums
{
    /// <summary>
    /// スキルの種類（CSVでは 0, 1, 2... の数字で指定します）
    /// </summary>
    public enum SkillType
    {
        MoveSpeedUp = 0,    // 移動速度アップ
        AttackPowerUp = 1,  // 攻撃力アップ
        FireRateUp = 2,     // 連射速度アップ（間隔短縮）
        ReloadSpeedUp = 3,  // リロード速度アップ（時間短縮）
        MaxAmmoUp = 4       // 最大弾数アップ
    }
}
```

次に `Scripts/InGame/Data` に `SkillData.cs` を作ります。<br>
**ファイル名： `SkillData.cs`**
``` cs
using System.Collections.Generic;
using UnityEngine;

namespace InGame.Data
{
    [Serializable]
    public class SkillDataRecord : IMasterData
    {
        [field: SerializeField] public ulong Id { get; private set; }
        [field: SerializeField] public string SkillName { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        
        // どのステータスを上げるか（SkillTypeの数字）
        [field: SerializeField] public int SkillType { get; private set; }
        
        // どれくらい上げるか（例：0.1 なら 10%アップ、5 なら 5発アップ）
        [field: SerializeField] public float Value { get; private set; }
    }

    [CreateAssetMenu(fileName = "SkillData", menuName = "ScriptableObjects/SkillData")]
    public class SkillData : ScriptableObject, IMasterDataContainer<SkillDataRecord>
    {
        [field: SerializeField] public List<SkillDataRecord> Records { get; private set; } = new List<SkillDataRecord>();
    }
}
```

💡 エディタでの作業手順（CSVの作成と変換）<br>
[ ] スプレッドシートなどで以下の内容の `SkillData.csv` を作成し、Unityに入れます。
```
Id,SkillName,Description,SkillType,Value
1,俊足,移動速度が10%アップ,0,0.1
2,怪力,攻撃力が20%アップ,1,0.2
3,早撃ち,連射間隔が10%短縮,2,0.1
4,早業,リロード時間が10%短縮,3,0.1
5,拡張マガジン,最大弾数が5発アップ,4,5
```
[ ] 第7週で導入した `Tools > CSVを一括でMasterDataに変換` を使って、SOに変換します！<br>
[ ] ※ `MasterDataAccessor.cs` の `InitializeAsync` に、`LoadAsync<SkillData, SkillDataRecord>("SkillData")` の読み込み処理を忘れずに追加してください。（ラベル設定も忘れずに！）

## 2. プレイヤーに「成長の受け皿（バフ）」を作る
マスターデータ（武器の元の攻撃力など）を直接書き換えてしまうと、次のプレイでも強いままになるバグが起きます。<br>
必ずプレイヤー側に「強化倍率（バフ）」の変数を作り、計算の瞬間に掛け合わせるようにします！<br>
**ファイル名： `PlayerController.cs`（追加・変更部分）**
``` diff
// ... (上部は省略) ...
namespace TPSRoguelite.InGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        // ... (中略) ...

        /// <summary>
        /// 射撃可能か
        /// </summary>
        private bool canShoot = true;

        /// <summary>
        /// 射撃のキャンセルトークン
        /// </summary>
        private CancellationTokenSource fireCts;

+       // --- スキルによる強化倍率（バフ） ---
+       private float moveSpeedBuff = 0f;
+       private float attackPowerBuff = 0f;
+       private float fireRateBuff = 0f;
+       private float reloadSpeedBuff = 0f;
+       private int maxAmmoBuff = 0;

        /// <summary>
        /// 次のレベルに必要な経験値
        /// </summary>
        private int RequiredExp => CurrentLevel * 5; // 例: レベル1なら5、レベル2なら10、レベル3なら15...

+        private int FinalAttackPower => currentWeapon != null ? Mathf.RoundToInt(currentWeapon.AttackPower * (1f + attackPowerBuff)) : 0;
+
+        private int FinalMaxAmmo => currentWeapon != null ? currentWeapon.MaxAmmo + maxAmmoBuff : 0;
+        private float FinalReloadTime => currentWeapon != null ? currentWeapon.ReloadTime * Mathf.Max(0.1f, 1f - reloadSpeedBuff) : 0f;
+        private float FinalFireRate => currentWeapon != null ? currentWeapon.FireRate * Mathf.Max(0.1f, 1f - fireRateBuff) : 0f;


        /// <summary>
        /// 外部（アニメーションやUIなど）に現在の速度を教えるために保持するVelocity
        /// </summary>
        public Vector3 CurrentVelocity { get; private set; }

        // ... (中略) ...

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
            CurrentLevel = 1;

+           // バフを初期化（リトライ時に強さが残らないようにする）
+           moveSpeedBuff = 0f;
+           attackPowerBuff = 0f;
+           fireRateBuff = 0f;
+           reloadSpeedBuff = 0f;
+           maxAmmoBuff = 0;

            if (levelUpText != null)
            {
                // レベルアップ時のテキストを非表示にする
                levelUpText.enabled = false;
            }

            // ... (中略) ...
        }

+       /// <summary>
+       /// スキルを適用する
+       /// </summary>
+       public void ApplySkill (SkillDataRecord skill)
+       {
+           switch ((SkillType)skill.SkillType)
+           {
+               case SkillType.MoveSpeedUp:
+                   moveSpeedBuff += skill.Value;
+                   break;
+       
+               case SkillType.AttackPowerUp:
+                   attackPowerBuff += skill.Value;
+                   break;
+       
+               case SkillType.FireRateUp:
+                   fireRateBuff += skill.Value;
+                   break;
+       
+               case SkillType.ReloadSpeedUp:
+                   reloadSpeedBuff += skill.Value;
+                   break;
+       
+               case SkillType.MaxAmmoUp:
+                   maxAmmoBuff += (int)skill.Value;
+                   CurrentAmmo += (int)skill.Value; // 増えた分だけ今すぐ弾を補充
+                   UpdateCurrentAmmoUI();
+                   break;
+       
+               default:
+                   Debug.LogWarning($"未定義のスキルタイプです: {skill.SkillType}");
+                   break;
+           }
+       
+           Debug.Log($"{skill.SkillName} を取得しました！");
+       }

        // --- 以下、既存の処理の「計算部分」にバフを掛け合わせるように修正 ---

        private void Move()
        {
            // (省略) ... moveDirection を計算した後
            
            // 物理演算で移動させる
-           Vector3 targetVelocity = moveDirection * MOVE_SPEED;
+           Vector3 targetVelocity = moveDirection * (MOVE_SPEED * (1f + moveSpeedBuff));
            rigidbody.linearVelocity = new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.z);

            // 外部（アニメーションやUIなど）に現在の速度を教えるためにプロパティを更新
            CurrentVelocity = rigidbody.linearVelocity;
        }

        /// <summary>
        /// セミオートの射撃処理
        /// </summary>
        private async UniTaskVoid ShootSemiAutoAsync(CancellationToken token) 
        {
            if (CurrentAmmo == 0) 
            {
                Reload();
                return;
            }

            canShoot = false;

            CurrentAmmo--;
            UpdateCurrentAmmoUI();
            Debug.Log($"セミオートで撃った！弾数：{CurrentAmmo}");
            Shoot();

-           await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: this.GetCancellationTokenOnDestroy());
+           await UniTask.Delay(TimeSpan.FromSeconds(FinalFireRate), cancellationToken: this.GetCancellationTokenOnDestroy());

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
                    Reload();
                    break;
                }

                CurrentAmmo--;
                UpdateCurrentAmmoUI();
                Shoot();
                Debug.Log($"バースト！残弾数：{CurrentAmmo}");

                await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInteval), cancellationToken: token);
            }

-           await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: this.GetCancellationTokenOnDestroy());
+           await UniTask.Delay(TimeSpan.FromSeconds(FinalFireRate), cancellationToken: this.GetCancellationTokenOnDestroy());
            canShoot = true;
        }

        private async UniTaskVoid ShootFullAutoAsync(CancellationToken token)
        {
            canShoot = false;

            while (!token.IsCancellationRequested) 
            {
                if (CurrentAmmo <= 0) 
                {
                    Reload();
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

-           await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: this.GetCancellationTokenOnDestroy());
+           await UniTask.Delay(TimeSpan.FromSeconds(FinalFireRate), cancellationToken: this.GetCancellationTokenOnDestroy());

            canShoot = true;
        }

        private void Shoot() 
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

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
-                   target.TakeDamage(currentWeapon.AttackPower);
+                   int finalDamage = Mathf.RoundToInt(currentWeapon.AttackPower * (1f + attackPowerBuff));
+                   target.TakeDamage(finalDamage);
                }
            }
        }

        private void OnReload(InputAction.CallbackContext context)
        {
-           if (isReloading || CurrentAmmo == currentWeapon.MaxAmmo)
+           if (isReloading || CurrentAmmo == FinalMaxAmmo)
            {
                return;
            }

            Reload();
        }

        private void Reload()
        {
            isReloading = true;

            if (reloadUI != null)
            {
                reloadUI.gameObject.SetActive(true);
            }

            if (reloadCircleImage != null)
            {
                reloadCircleImage.fillAmount = 0f;
            }
        
-           DOVirtual.Float(0f, 1f, currentWeapon.ReloadTime, UpdateReloadUI).SetEase(Ease.Linear).OnComplete(FinishReload);
+           float finalReloadTime = currentWeapon.ReloadTime * Mathf.Max(0.1f, 1f - reloadSpeedBuff);
+           DOVirtual.Float(0f, 1f, FinalReloadTime, UpdateReloadUI).SetEase(Ease.Linear).OnComplete(FinishReload);
        }

        private void UpdateCurrentAmmoUI()
        {
            if (ammoText != null && currentWeapon != null)
            {
-               ammoText.text = $"{CurrentAmmo}/{currentWeapon.MaxAmmo}";
+               ammoText.text = $"{CurrentAmmo}/{FinalMaxAmmo}";
            }
        }

        /// <summary>
        /// リロードUIの更新
        /// </summary>
        private void UpdateReloadUI(float value)
        {
            if (reloadCircleImage != null)
            {
                reloadCircleImage.fillAmount = value;
            }
        }

        /// <summary>
        /// リロード完了時の処理
        /// </summary>
        private void FinishReload()
        {
            if (reloadUI != null)
            {
                reloadUI.SetActive(false);
            }

-           CurrentAmmo = currentWeapon.MaxAmmo;
+           CurrentAmmo = FinalMaxAmmo;
            UpdateCurrentAmmoUI();
            isReloading = false;
        }
```

## 3. レベルアップを管理する司令塔「LevelUpManager」
スキル画面を表示し、CSVから3つのスキルをランダムに選び、ボタンにセットする司令塔を作ります。<br>
**ファイル名： `LevelUpManager.cs`**
``` cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using InGame.Data;
using InGame.System;
using TPSRoguelite.InGame.Player;

namespace TPSRoguelite.InGame.Manager
{
    // ボタンとテキストをセットで管理するためのクラス
    [System.Serializable]
    public class SkillButtonUI
    {
        public Button button;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descText;
    }

    public class LevelUpManager : MonoBehaviour
    {
        public static LevelUpManager Instance { get; private set; }

        [Header("UI設定")]
        [SerializeField] private GameObject skillSelectPanel;
        [SerializeField] private SkillButtonUI[] skillButtons = new SkillButtonUI[3];

        private PlayerInputActions inputActions;
        private PlayerController playerController;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;
            if (skillSelectPanel != null)
            {
                skillSelectPanel.SetActive(false);
            }
        }

        /// <summary>
        /// レベルアップ時の処理を行うメソッド
        /// </summary>
        public void OnLevelUp(PlayerInputActions currentInput, PlayerController player)
        {
            inputActions = currentInput;
            playerController = player;

            // スキルをランダムに3つ選択してUIに表示する
            var allSkills = MasterDataAccessor.Instance.GetAll<SkillDataRecord>().ToList();
            var chosenSkills = allSkills.OrderBy(v => Random.Shared.Next()).Take(3).ToList();
            // .NET (.NET 5以前) の場合
            // var chosenSkills = allSkills.OrderBy(v => System.Guid.NewGuid()).Take(3).ToList();

            // UIにスキル情報を表示する
            for (int i = 0; i < 3; i++)
            {
                var skill = chosenSkills[i];
                var ui = skillButtons[i];

                ui.nameText.text = skill.SkillName;
                ui.descText.text = skill.Description;

                // 古いリスナーを削除してから新しいリスナーを追加する
                ui.button.onClick.RemoveAllListeners();
                ui.button.onClick.AddListener(() => OnSkillSelected(skill));
            }

            // 画面を表示して時間を止める
            if (skillSelectPanel != null)
            {
                 skillSelectPanel.SetActive(true);
            }

            Time.timeScale = 0f;

            // マウスカーソルを解放し、ActionMapを切り替える
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (inputActions != null)
            {
                inputActions.Player.Disable();
            }
        }

        /// <summary>
        /// スキルが選択されたときの処理を行うメソッド
        /// </summary>
        private void OnSkillSelected(SkillDataRecord selectedSkill)
        {
            // スキルをプレイヤーに付与する
            if (playerController != null)
            {
                playerController.ApplySkill(selectedSkill);
            }

            // 画面を非表示にして時間を再開する
            if (skillSelectPanel != null)
            {
                skillSelectPanel.SetActive(false);
            }

            Time.timeScale = 1f;

            // マウスカーソルをロックし、ActionMapを切り替える
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (inputActions != null)
            {
                inputActions.Player.Enable();
            }
        }
    }
}
```
※ `PlayerController.cs` の `ShowLevelUpTextAsync()` メソッドから、`OnLevelUp` を呼びます。<br>
**ファイル名： `PlayerController.cs`（追加・変更部分）**
``` diff
// ... (上部は省略) ...
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
+using TPSRoguelite.InGame.Manager;

namespace TPSRoguelite.InGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        // ... (中略) ...

        /// <summary>
        /// レベルアップの文字を表示する非同期処理
        /// </summary>
        private async UniTaskVoid ShowLevelUpTextAsync()
        {
            if (levelUpText == null)
            {
                return;
            }

            levelUpText.enabled = true;
            levelUpText.SetText($"Level Up!\n<size=50%>Lv.{CurrentLevel}</size>");

            // 2秒間表示した後に非表示にする
            await UniTask.Delay(TimeSpan.FromSeconds(LEVEL_UP_EFFECT_DURATION), cancellationToken: this.GetCancellationTokenOnDestroy());

            levelUpText.enabled = false;

+           LevelUpManager.Instance.OnLevelUp(inputActions, this);
        }
    }
}
```

カメラの移動も停止します<br>
**ファイル名： `CameraController.cs`（追加・変更部分）**
``` diff
using UnityEngine;

namespace TPSRoguelite.InGame.Camera 
{
    public class CameraController : MonoBehaviour 
    {
        // ... (中略) ...

        private void OnDisable() 
        {
            inputActions.Disable();
        }

        private void Update() 
        {
+           if (Time.timeScale == 0f)
+           {
+               return;
+           }

            // マウスの移動量を取得
            lookInput = inputActions.Player.Look.ReadValue<Vector2>();

            // 感度を掛けて現在の角度に足し引きする
            currentYaw += lookInput.x * lookSensitivity;
            currentPitch -= lookInput.y * lookSensitivity;

            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        // ... (中略) ...
    }
}
```

## 4. UIの作成と連携（最後の仕上げ！）
💡 エディタでの作業手順<br>
[ ] Hierarchyの `Canvas` に、画面全体を覆う黒半透明の `Panel`（名前：`SkillSelectPanel`）を作ります。<br>
[ ] その中に `Button - TextMeshPro` を作り、大きさを整えます。<br>
[ ] ボタンの中に、スキル名用のTextと、説明文用のTextの2つの `TextMeshPro` を配置します。<br>
[ ] このボタンを複製して、横に3つ並べます。ボタンの Transition設定で、Highlighted Color（マウスを乗せた時の色）を明るくしておきましょう。<br>
[ ] 空のオブジェクト `LevelUpManager` を作成し、スクリプトをアタッチします。<br>
[ ] Inspectorの `Skill Buttons` を3つに展開し、先ほど作った3つのボタンと、それぞれの「名前Text」「説明Text」を1セットずつ枠にドラッグ＆ドロップで割り当てます。<br>
[ ] 最後に `SkillSelectPanel` を非表示にして、ゲームをプレイしてみましょう！<br>
レベルアップの瞬間、時間が止まり、3つの能力がランダムに表示されます。**マウスで選んでクリックすると、見事その能力が強化されてゲームが再開されれば大成功です！**
