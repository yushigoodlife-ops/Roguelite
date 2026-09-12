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
using TPSRoguelite.InGame.Manager;

namespace TPSRoguelite.InGame.Player {

    public class PlayerController : MonoBehaviour, IDamageable {
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

        private const float LEVEL_UP_EFFECT_DURATION = 2f;

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
        /// 武器のID（デフォルトは1）
        /// </summary>
        [SerializeField] private ulong weaponId = 1;

        /// <summary>
        /// マズルフラッシュのエフェト
        /// </summary>
        [SerializeField] private ParticleSystem muzzleFlash;

        /// <summary>
        /// 武器の名前
        /// </summary>
        [SerializeField] private TextMeshProUGUI weaponName;

        /// <summary>
        /// 弾のテキスト
        /// </summary>
        [SerializeField] private TextMeshProUGUI ammoText;

        /// <summary>
        /// リロード中のテキストと画像をまとめたオブジェクト
        /// </summary>
        [SerializeField] private GameObject reloadUI;

        /// <summary>
        /// リロード中の時間が分かるサークル画像
        /// </summary>
        [SerializeField] private Image reloadCircleImage;

        [SerializeField] private Slider expBar;
        [SerializeField] private TextMeshProUGUI levelUpText;
        [SerializeField] private ParticleSystem levelUpEffect;
        [SerializeField] private Slider hpBar;

        /// <summary>
        /// 武器のデータ
        /// </summary>
        private WeaponDataRecord currentWeapon;

        /// <summary>
        /// 自動生成されたInputクラス
        /// </summary>
        private PlayerInputActions inputActions;

        /// <summary>
        /// 入力方向
        /// </summary>
        private Vector2 moveInput = Vector2.zero;

        /// <summary>
        /// 移動方向のベクトル
        /// </summary>
        private Vector3 moveDirection;

        /// <summary>
        /// カメラのトランスフォーム
        /// </summary>
        private Transform mainCameraTransform;

        /// <summary>
        /// リロードしているか
        /// </summary>
        private bool isReloading;

        /// <summary>
        /// 射撃可能か
        /// </summary>
        private bool canShoot = true;

        /// <summary>
        /// 射撃のキャンセルトークン
        /// </summary>
        private CancellationTokenSource fireCts;

        // --- スキルによるバフ ---
        private float moveSpeedBuf = 0f;
        private float attackPowerBuf = 0f;
        private float fireRateBuf = 0f;
        private float reloadSpeedBuf = 0f;
        private int maxAmmoBuf = 0;

        /// <summary>
        /// 外部（アニメーションやUIなど）に現在の速度を教えるために保持するVelocity
        /// </summary>
        public Vector3 CurrentVelocity { get; private set; }

        /// <summary>
        /// 現在の弾数
        /// </summary>
        public int CurrentAmmo { get; private set; }

        public int CurrentExp { get; private set; }

        public int CurrentLevel { get; private set; }

        public int MaxHP { get; private set; } = 100;
        public int CurrentHP { get; private set; }

        private int RequiredExp => CurrentLevel * 5;

        // todo: floatに直す
        private int FinalAttackPower => currentWeapon != null 
            ? Mathf.RoundToInt(currentWeapon.AttackPower * (1f + attackPowerBuf))
            : 0;

        private int FinalMaxAmmo => currentWeapon != null
            ? currentWeapon.MaxAmmo + maxAmmoBuf
            : 0;

        private float FinalReloadTime => currentWeapon != null
            ? currentWeapon.ReloadTime * Mathf.Max(0.1f, 1f - reloadSpeedBuf)
            : 0f;

        private float FinalFireRate => currentWeapon != null
            ? currentWeapon.FireRate * Mathf.Max(0.1f, 1f - fireRateBuf)
            : 0f;

        private void Awake() {
            gameObject.SetActive(false);
        }

        public void Setup()
        {
            TakeWeapon(weaponId);

            moveSpeedBuf = 0f;
            attackPowerBuf = 0f;
            fireRateBuf = 0f;
            reloadSpeedBuf = 0f;
            maxAmmoBuf = 0;

            inputActions = new PlayerInputActions();
            inputActions.Player.Fire.performed += OnFire; // 押し続けていると呼ばれる
            inputActions.Player.Fire.canceled += OnFire;
            inputActions.Player.Reload.performed += OnReload;

            if (UnityEngine.Camera.main != null) {
                mainCameraTransform = UnityEngine.Camera.main.transform;
            } else {
                Debug.LogError("Main Cameraが見つかりません。");
            }

            if (reloadUI != null)
            {
                reloadUI.SetActive(false);
            }

            CurrentExp = 0;
            CurrentLevel = 1;
            if (levelUpText != null)
            {
                levelUpText.enabled = false;
            }

            CurrentHP = MaxHP;
            UpdateHpBar();

            UpdateExpUI();

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


        private void Move() 
        {
            if (rigidbody == null || mainCameraTransform == null)
            {
                return;
            }

            Vector3 cameraForward = mainCameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            if (cameraForward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
                rigidbody.rotation = Quaternion.Slerp(rigidbody.rotation, targetRotation, ROTATE_SPEED * Time.fixedDeltaTime);
            }
              
            // 入力がない場合はピタッと止める
            if (moveInput == Vector2.zero) {
                rigidbody.linearVelocity = new Vector3(0f, rigidbody.linearVelocity.y, 0f);
                CurrentVelocity = Vector3.zero;
                return;
            }

            // カメラ基準の計算に変更
            Vector3 cameraRight = mainCameraTransform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

            float finalMoveSpeed = MOVE_SPEED * (1f + moveSpeedBuf);
            Vector3 targetVelocity = moveDirection * finalMoveSpeed;
            rigidbody.linearVelocity = new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.z);

            // 外部（アニメーションやUIなど）に現在の速度を教えるためにプロパティを更新
            CurrentVelocity = rigidbody.linearVelocity;
        }

        private void OnFire(InputAction.CallbackContext context)
        {
            if (context.performed) 
            {
                if (!canShoot || isReloading || currentWeapon == null) 
                {
                    return;
                }

                fireCts = new CancellationTokenSource();
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(fireCts.Token, this.GetCancellationTokenOnDestroy());
                
                switch ((FireType)currentWeapon.WeaponFireType)
                {
                    case Enum.FireType.SemiAuto:
                        ShootSemiAutoAsync(this.GetCancellationTokenOnDestroy()).Forget();
                        break;

                    case Enum.FireType.Burst:
                        ShootBurstAsync(this.GetCancellationTokenOnDestroy()).Forget();
                        break;

                    case Enum.FireType.FullAuto:
                        ShootFullAutoAsync(linkedCts.Token).Forget();
                        break;

                    default:
                        Debug.LogWarning($"割り当てていない射撃タイプがあります。{currentWeapon.WeaponFireType}");
                        break;
                }
            }

            if (context.canceled) 
            {
                fireCts?.Cancel();
                fireCts?.Dispose();
                fireCts = null;
            }
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

            await UniTask.Delay(System.TimeSpan.FromSeconds(FinalFireRate), cancellationToken: token);

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

            await UniTask.Delay(TimeSpan.FromSeconds(FinalFireRate), cancellationToken: token);
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

            await UniTask.Delay(TimeSpan.FromSeconds(FinalFireRate), cancellationToken: this.GetCancellationTokenOnDestroy());

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
                    target.TakeDamage(FinalAttackPower);
                }
            }
        }

        private void OnReload(InputAction.CallbackContext context)
        {
            if (isReloading || CurrentAmmo == FinalMaxAmmo) {
                return;
            }

            Reload();
        }

        private void Reload()
        {
            isReloading = true;

            if (reloadUI != null)
            {
                reloadUI.SetActive(true);
            }

            if (reloadCircleImage != null)
            {
                reloadCircleImage.fillAmount = 0f;
            }

            float finalReloadTime = currentWeapon != null
                ? currentWeapon.ReloadTime * Mathf.Max(0.1f, 1f - reloadSpeedBuf)
                : 0f;
            DOVirtual.Float(0f, 1f, finalReloadTime, UpdateReloadUI)
                .SetEase(Ease.Linear).OnComplete(FinishReload);
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

        private void UpdateWeaponUI()
        {
            if (weaponName != null)
            {
                weaponName.SetText(currentWeapon.WeaponName);

                // 色で武器のタイプが分かる
                switch ((FireType)currentWeapon.WeaponFireType)
                {
                    case FireType.SemiAuto:
                        weaponName.color = Color.white;
                        break;

                    case FireType.Burst:
                        weaponName.color = Color.yellow;
                        break;

                    case FireType.FullAuto:
                        weaponName.color = Color.red;
                        break;
                }
            }

            UpdateCurrentAmmoUI();
        }

        private void UpdateCurrentAmmoUI() 
        {
            if (ammoText != null)
            {
                ammoText.SetText($"{CurrentAmmo}/{FinalMaxAmmo}");
            }
        }

        private void UpdateReloadUI(float value)
        {
            if (reloadCircleImage != null)
            {
                reloadCircleImage.fillAmount = value;
            }
        }

        private void FinishReload()
        {
            if (reloadUI != null)
            {
                reloadUI.SetActive(false);
            }

            CurrentAmmo = FinalMaxAmmo;
            UpdateCurrentAmmoUI();
            isReloading = false;
        }

        public void AddExp(int amount)
        {
            CurrentExp += amount;

            if (CurrentExp >= RequiredExp)
            {
                LevelUp();
            }

            UpdateExpUI();
        }

        private void UpdateExpUI()
        {
            if (expBar != null)
            {
                expBar.value = (float)CurrentExp / RequiredExp;
            }
        }

        private void LevelUp()
        {
            CurrentExp -= RequiredExp;
            CurrentLevel++;

            if (levelUpEffect != null)
            {
                levelUpEffect.Play();
            }

            ShowLevelUpTextAsync().Forget();
        }

        private async UniTaskVoid ShowLevelUpTextAsync()
        {
            if (levelUpText == null)
            {
                return;
            }

            levelUpText.enabled = true;
            levelUpText.SetText($"Level Up!\n<size=50%>Lv.{CurrentLevel}</size>");

            await UniTask.Delay(TimeSpan.FromSeconds(LEVEL_UP_EFFECT_DURATION),
                cancellationToken: this.GetCancellationTokenOnDestroy());

            levelUpText.enabled = false;

            LevelUpManager.Instance.OnLevelUp(inputActions, this);
        }

        public void ApplySkill(SkillDataRecord skill)
        {
            switch ((SkillType)skill.SkillType)
            {
                case SkillType.MoveSpeedUp:
                    moveSpeedBuf += skill.Value;
                    break;

                case SkillType.AttackPowerUp:
                    attackPowerBuf += skill.Value;
                    break;

                case SkillType.FireRateUp:
                    fireRateBuf += skill.Value;
                    break;

                case SkillType.ReloadSpeedUp:
                    reloadSpeedBuf += skill.Value;
                    break;

                case SkillType.MaxAmmoUp:
                    maxAmmoBuf += (int)skill.Value;
                    UpdateCurrentAmmoUI();
                    break;

                case SkillType.TakeWeapon:
                    TakeWeapon((ulong)skill.Value);
                    break;
            }
        }

        private void TakeWeapon(ulong id)
        {
            currentWeapon = MasterDataAccessor.Instance.GetById<WeaponDataRecord>(id);

            if (currentWeapon != null)
            {
                CurrentAmmo = FinalMaxAmmo;
                UpdateWeaponUI();
            } else {
                Debug.LogError("WeaponDataがありません。");
            }
        }

        private void UpdateHpBar()
        {
            if (hpBar != null) 
            {
                hpBar.value = (float)CurrentHP / MaxHP;
            }
        }

        private void Die()
        {
            gameObject.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }

        public void TakeDamage(int damageAmount)
        {
            if (damageAmount <= 0 || CurrentHP <= 0)
            {
                return;
            }

            CurrentHP -= damageAmount;
            UpdateHpBar();

            if (CurrentHP <= 0)
            {
                Die();
            }
        }
    }
}
