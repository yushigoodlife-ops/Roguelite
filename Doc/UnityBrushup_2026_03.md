# 第3週：TPSカメラ制御とレーザーポインターの実装
1. プログラミング（3D数学）の力で、プレイヤーを追いかける「TPSカメラ」を制御する。
2. カメラが向いている方向へプレイヤーを移動させる。
3. `LineRenderer` を用いて、狙っている場所を示す「レーザーポインター」を描画する。

---

## 1. TPSカメラを制御する

### 1-1. マウスの動きを読み取る「Look」を追加しよう
カメラのプログラムを書く前に、マウスを動かした量（上下左右）をUnityに教えてあげる設定が必要です。
前回作った入力設定ファイル「PlayerInputActions」をダブルクリックして開き、以下の設定を追加しましょう。
1. Actionsのリストの右にある「＋」ボタンを押し、新しいアクションを作り、名前を「Look」にします。
2. 作った「Look」を選択し、右側の Action Type を「Value」に、Control Type を「Vector 2」に変更します。
3. Lookの下にある「」を選択し、右側の Path をクリックして「Mouse」の中にある「Delta」を選びます。
4. 設定画面の右上、または左上にある「Save Asset」ボタンを忘れずに押して保存し、画面を閉じます。

### 1-2. カメラを制御する専用のスクリプトを作成する

1. ScriptフォルダにあるInGameフォルダに `Camera` という名前のフォルダを追加しましょう。
2. Cameraフォルダの中に `CameraController.cs` という名前のスクリプトを作成します。

**ファイル名：`CameraController.cs`**
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace TPSRoguelite.InGame.Camera
{
    public class CameraController : MonoBehaviour
    {
        /// <summary>
        /// マウス感度
        /// </summary>
        private const float LOOK_SENSITIVITY = 0.2f;

        /// <summary>
        /// プレイヤーからの距離
        /// </summary>
        private const float DISTANCE = 5.0f;

        /// <summary>
        /// プレイヤーの少し上を狙う高さ
        /// </summary>
        private const float HEIGHT_OFFSET = 1.5f;

        /// <summary>
        /// 縦の最小角度
        /// </summary>
        private const float MIN_PITCH = -10f;

        /// <summary>
        /// 縦の最大角度
        /// </summary>
        private const float MAX_PITCH = 60f;

        /// <summary>
        /// 追従するターゲット
        /// </summary>
        [SerializeField] private Transform target;
    
        /// <summary>
        /// 自動生成されたクラス
        /// </summary>
        private PlayerInputActions inputActions;

        /// <summary>
        /// 移動量
        /// </summary>
        private Vector2 lookInput = Vector2.zero;

        /// <summary>
        /// 横の回転角度（Y軸回転）
        /// </summary>
        private float currentYaw = 0f;

        /// <summary>
        /// 縦の回転角度（X軸回転）
        /// 初期値を20にしておく
        /// </summary>
        private float currentPitch = 20f;

        private void Awake()
        {
            inputActions = new PlayerInputActions();

            // マウスカーソルを画面中央にロックして非表示にする
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
            currentYaw += lookInput.x * LOOK_SENSITIVITY;
            currentPitch -= lookInput.y * LOOK_SENSITIVITY;

            // 上下の回転限界を設定し、カメラが反転するのを防ぐ
            currentPitch = Mathf.Clamp(currentPitch, MIN_PITCH, MAX_PITCH);
        }
    
        private void LateUpdate()
        {
            // カメラの移動は、プレイヤーの移動が終わった後(LateUpdate)に行う

            // ターゲットが設定されていない場合はエラーを回避
            if (target == null)
            {
                return;
            }

            // 注視点を計算（足元ではなく頭のあたり）
            Vector3 targetPosition = target.position + Vector3.up * HEIGHT_OFFSET;

            // 角度（ピッチとヨー）をQuaternionに変換
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        
            // 注視点から、計算した角度の後ろ方向へ距離分だけ離した位置を計算
            Vector3 cameraPosition = targetPosition - (rotation * Vector3.forward * DISTANCE);

            // カメラの位置と回転を確定
            transform.position = cameraPosition;
            transform.rotation = rotation;
        }
    }
}
```

### 1-3. Main Cameraのセットアップ
スクリプトが書けたら、実際にUnityの画面でカメラにセットしましょう。
1. Hierarchy（左のリスト）にある「Main Camera」を選択します。
2. 今書いた「CameraController」スクリプトを、Main Camera の Inspector（右の画面）の一番下へドラッグ＆ドロップしてアタッチします。
3. Main Camera の Inspector を見ると、CameraController の中に「Target」という空っぽの枠ができています。
4. Hierarchyにある自分の「Player」オブジェクトを、その「Target」の枠にドラッグ＆ドロップしてセットします。

### 1-4. 「ベクトル」のイメージを持とう<br>
ゲームプログラミングにおいて、「ベクトル（Vector3）」という言葉がたくさん出てきます。難しく聞こえるかもしれませんが、ゲームの世界ではシンプルに「空間に浮かぶ、向きと長さを持った矢印*だと想像してください。<br>
たとえば、「北（向き）へ、5メートル（長さ）進む」という指示そのものがベクトルです。<br>
Unityでは、この矢印の足し算や引き算をすることで、キャラクターを動かしたり、カメラの位置を決めたりします。

### 1-5. カメラの計算：なぜ引き算をしているの？
カメラのスクリプトにある、以下の計算について解説します。<br>
`cameraPosition = targetPosition - (rotation * Vector3.forward * DISTANCE);`
<br>
ここは「カメラマン（カメラ）に、どこに立ってほしいかを指示する」計算です。<br>
`targetPosition` は、プレイヤーの頭のあたりの位置です。そこに、カメラの角度の方向へ距離分伸びる矢印（前方向）を計算しています。<br>
<br>
では、なぜプレイヤーの位置から、この矢印を「引き算（マイナス）」しているのでしょうか。<br>
もし足し算をしてしまうと、カメラマンはプレイヤーの「5メートル前」に立ってしまい、顔のどアップを映すことになります。だから、プレイヤーの位置から「前の矢印」をマイナス（逆方向に反転）することで、「プレイヤーの5メートル後ろに立ってね」という計算をしているのです。

### 1-6. なぜUpdate、FixedUpdate、LateUpdateを使い分けるのか？
Unityでプログラミングをしていると、「毎フレーム実行されるメソッド」が3種類も登場します。TPSのキャラクターとカメラを滑らかに動かすためには、この3つの「実行される順番と役割」を正しく分業させる必要があります。

それぞれを「映画の撮影」や「人間の体」に例えて理解しましょう。
1. Update
   ・呼ばれるタイミング：画面のコマ（フレーム）が切り替わるたび。PCの性能によって間隔がバラバラ。
   ・役割：「入力の受付」や「タイマー」など
   ・例え：人間の「目」や「脳」です。プレイヤーがいつ「Wキー」を押すか、いつ「クリック（射撃）」するかは予測できないため、常に最速のスピードで監視し続ける必要があります。そのため、キーボードやマウスの入力は必ず Update で受け取ります。
2. FixedUpdate
   ・呼ばれるタイミング：PCの性能に関係なく、常に「一定の時間間隔（標準では0.02秒ごと）」でキッチリ呼ばれる
   ・役割：「Rigidbody（物理演算）」を使った移動や力の計算
   ・例え：人間の「心臓の鼓動」や「地球の重力」です。もし物理演算を Update（間隔がバラバラ）で行うと、PCが重くなった瞬間にキャラクターが壁をすり抜けたり、ジャンプ力が変わったりするバグが起きます。そのため、Rigidbodyを動かす処理は、必ず一定リズムの FixedUpdate に書きます。
3. LateUpdate
   ・呼ばれるタイミング：Update と FixedUpdate の処理が「すべて完全に終わった後」に呼ばれる。
   ・役割：「カメラの追従」など、他のオブジェクトの動きに合わせて動く処理
   ・例え：映画の「カメラマン」です

**★なぜカメラは LateUpdate なのか？（超重要）**<br>
もしカメラの追従を Update で行ってしまうと、何が起きるでしょうか。<br>
Unityの内部では「カメラがプレイヤーの位置へ移動する処理」と、「プレイヤーが前へ移動する処理」が、どっちが先に実行されるか分かりません（順番がランダムになります）。<br>
「カメラが移動 → プレイヤーが移動」という順序になったフレームでは、カメラが置いてけぼりになり、一瞬だけプレイヤーが画面の奥へズレます。これが毎秒連続で起きることで、画面がガクガク・ブルブルと激しく震える「カメラのストッター（カクつき）現象」が発生します。<br>
これを防ぐために、カメラマンには「Late（遅れて）」動いてもらいます。<br>
プレイヤーが移動をすべて終えて、立ち位置が完全に確定したあとに、カメラマンがそこへサッと回り込んで撮影する（LateUpdate）。こうすることで、プロのゲームのようなヌルッと滑らかなカメラワークが実現するのです。<br>

試しに `LateUpdate` の部分をわざと `Update` に書き換えてみましょう。<br>
プレイヤーを動かして画面がガクガク震えるのを体験してみてください。<br>
体験したら `LateUpdate` に戻してください。<br>

---

## 2. プレイヤーの移動処理の改修

### 2-1. プレイヤーの移動処理を「カメラ基準」に変更
前回作成した `PlayerController.cs` を、カメラの向いている方向に進むように書き換えます。

**ファイル名：`PlayerController.cs`**
```diff
  using UnityEngine;
  using UnityEngine.InputSystem;

  public class PlayerController : MonoBehaviour
  {
      private const float MOVE_SPEED = 5.0f;

+     /// <summary>
+     /// 回転速度
+     /// </summary>
+     private const float ROTATION_SPEED = 10.0f;

      [SerializeField] private Rigidbody rigidBody;
      private PlayerInputActions inputActions;
      private Vector2 moveInput;

+     /// <summary>  
+     /// カメラのトランスフォーム
+     /// </summary>
+     private Transform mainCameraTransform;

      public Vector3 CurrentVelocity { get; private set; }

      private void Awake()
      {
          if (rigidBody == null)
          {
                Debug.LogError("Rigidbodyがありません！");
          }

+         if (Camera.main != null)
+         {
+             mainCameraTransform = Camera.main.transform;
+         }
+         else
+         {
+             Debug.LogError("Main Cameraが見つかりません！");
+         }

          inputActions = new PlayerInputActions();
      }

      // (OnEnable, OnDisable, Update, FixedUpdate は省略)

      private void Move()
      {
          if (moveInput == Vector2.zero)
          {
              rigidBody.velocity = new Vector3(0f, rigidBody.velocity.y, 0f);
              CurrentVelocity = Vector3.zero;
              return;
          }

-         // 以前の絶対方向への移動ロジック
-         Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
-         Vector3 targetVelocity = moveDirection * MOVE_SPEED;
-         rigidBody.velocity = new Vector3(targetVelocity.x, rigidBody.velocity.y, targetVelocity.z);

+         // カメラ基準の計算に変更
+         Vector3 cameraForward = mainCameraTransform.forward;
+         Vector3 cameraRight = mainCameraTransform.right;
+ 
+         // 空や地面に向かって移動しないよう、Y軸を水平に補正
+         cameraForward.y = 0f;
+         cameraRight.y = 0f;
+         cameraForward.Normalize();
+         cameraRight.Normalize();
+ 
+         Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
+ 
+         // キャラクターを進行方向へ滑らかに振り向かせる
+         Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
+         rigidBody.rotation = Quaternion.Slerp(rigidBody.rotation, targetRotation, ROTATION_SPEED * Time.fixedDeltaTime);
+ 
+         // Y軸の速度（落下など）は現在の物理演算の値を維持し、XとZのみ上書きする
+         Vector3 targetVelocity = moveDirection * MOVE_SPEED;
+         rigidBody.velocity = new Vector3(targetVelocity.x, rigidBody.velocity.y, targetVelocity.z);

          CurrentVelocity = rigidBody.velocity;
      }
  }
```

### 2-2. プレイヤーの移動計算：なぜYを0にするの？<br>
プレイヤーのスクリプトでは、カメラの向きの「y」をわざと「0」にしています。（cameraForward.y = 0f;）<br>
これは、「プレイヤーを空に飛ばさない（地面にめり込ませない）ための安全対策」です。<br>
カメラはプレイヤーを見下ろしていることが多いので、カメラの「前」という矢印は、実は「斜め下（地面の方向）」を向いています。<br>
そのまま進ませると地面にめり込んでしまうため、矢印のY軸（高さ）成分だけを0にリセットしてパタンと倒し、「地面と平行な、正しい前方向の矢印」に作り直しているのです。

### 2-3. Normalize（正規化）
Normalizeとは、矢印の向いている方向はそのままにして、長さをピッタリ「1」に整える魔法の処理です。<br>
Y軸を0にして矢印を倒すと、元の矢印より少し長さが短くなってしまいます。長さがバラバラのまま移動スピードを掛けると、カメラの角度によって歩く速度が変わってしまうというバグが起きます。<br>
また、斜め移動をしたとき、前方向と右方向の矢印が合体して長さが約1.4倍に伸び、斜めに歩くと異常に速いバグが起きます。<br>
これらを防ぐために、移動する方向の矢印を作ったら、必ず最後に Normalize をして長さを「1」にリセットします。そこに定数（MOVE_SPEEDなど）を掛けることで、どんな方向へも常に一定のスピードで綺麗に動くようになるのです。

---

## 3. レーザーポインターの実装
カメラが向いている方向を視覚的にわかりやすくするため、赤いレーザー（光線）を描画します。

### 3-1. LineRendererのセットアップ（Unityエディタ）
1. `Player` オブジェクトの子に空のオブジェクトを作成し、名前を `WeaponOrigin（銃口）`にします。
2. `WeaponOrigin` に `Line Renderer` コンポーネントを追加します。
3. `Line Renderer` の設定を変更します：
   ・ `Width` を `0.05` など細くする。
   ・ `Materials` に赤いマテリアルを設定する。
   ・ `Cast Shadows` を `Off` にする（処理を軽くするため）。

### 3-2. PlayerControllerの改修（レーザー描画）

**ファイル名：`PlayerController.cs`**
```diff
public class PlayerController : MonoBehaviour
{
      private const float MOVE_SPEED = 5.0f;
      private const float ROTATION_SPEED = 10.0f;

+     /// <summary>
+     /// レーザーポインターの描画距離
+     /// </summary>      
+     private const float LASER_MAX_DISTANCE = 50.0f;

      private Rigidbody rigidBody;
      private PlayerInputActions inputActions;
      private Vector2 moveInput;
      private Transform mainCameraTransform;

+     /// <summary>
+     /// レーザーポインターの描画コンポーネント
+     /// </summary>
+     [SerializeField] private LineRenderer laserLineRenderer;

+     /// <summary>
+     /// 銃口の位置
+     /// </summary>
+     [SerializeField] private Transform weaponOrigin;

      public Vector3 CurrentVelocity { get; private set; }

      // (Awake等の処理は省略)

      private void Update()
      {
          moveInput = inputActions.Player.Move.ReadValue<Vector2>();
+         DrawLaserPointer();
      }

+     /// <summary>
+     /// レーザーを描画
+     /// </summary>
+     private void DrawLaserPointer()
+     {
+         if (laserLineRenderer == null || weaponOrigin == null || mainCameraTransform == null)
+         {
+            return;
+         }
+ 
+         laserLineRenderer.SetPosition(0, weaponOrigin.position);
+
+         // カメラの中央から真っ直ぐ前へ光線を飛ばす
+         Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);
+ 
+         // 光線が何かに当たったか判定
+         if (Physics.Raycast(ray, out RaycastHit hitInfo, LASER_MAX_DISTANCE))
+         {
+             laserLineRenderer.SetPosition(1, hitInfo.point);
+         }
+         else
+         {
+             // 何も当たらなかったら、最大距離の場所を終点にする    
+             laserLineRenderer.SetPosition(1, ray.GetPoint(LASER_MAX_DISTANCE));
+         }
+     }

      // (FixedUpdateとMoveは省略)
}
```

### 3-EXTRA. ★チャレンジ課題（任意）
レーザーが敵（特定のTag等）に当たっている時だけ、レーザーの色を「黄色」に変化させる処理を DrawLaserPointer 内に書いてみよう。
