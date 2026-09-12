# 第2週：キャラクターを動かそう！「PlayerControllerと新しい入力システム」

## 本日の目標
前回、ゲームの舞台となる3つのシーンを用意しました。今日はいよいよ主役の「プレイヤー」を登場させ、実際にキーボードで動かしてみましょう！

1. **コードの整理（namespace）**：クラスに「住所」をつけて整理する考え方を学び、これから書くコードに最初から適用する。
2. **物理演算での移動（Rigidbody）**：`PlayerController` を作成し、プレイヤーをWASDキーで動かす。
3. **入力の仕組みを知る**：今使っている「古い入力方式」の弱点を理解する。
4. **新しい入力システム（Input System）の導入**：もっと柔軟で拡張しやすい入力管理に切り替える。

---

## 1. コードを整理しよう：namespaceの考え方

### 1-1. なぜ「namespace」が必要なのか？
皆さんの学校に「佐藤さん」が3人いたら、名前を呼ぶだけでは誰のことかわかりませんよね？<br>
そこで、「1年A組の佐藤さん」「サッカー部の佐藤さん」というように、「所属」を付けて区別するはずです。

プログラミングの **namespace（名前空間）** もこれと同じです。<br>
**・名前の衝突を防ぐ:** 他の人が作ったプログラムや、Unityの便利な機能と同じ名前のプログラムを作ってしまったとき、namespace（所属）が違うことで、コンピュータは正しく区別できます。<br>
**・整理整頓:** プログラムを「プレイヤー用」「敵用」「システム用」といったグループに分けることで、どこに何があるか探しやすくなります。

今日からプログラムを書くときは、最初から namespace で「住所」を付けて整理していきましょう。

### 1-2. namespaceの書き方
**書き方のポイント:**<br>
・`namespace プロジェクト名.カテゴリ名 { ... }` という形式で、クラス全体を囲みます。<br>
・プロジェクト名は `TPSRoguelite` とします。<br>
・カテゴリ名は、フォルダの階層に合わせます。今日作るプレイヤー関連のプログラムは `InGame.Player` とします。

```cs
using UnityEngine;

namespace TPSRoguelite.InGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        // クラスの中身
    }
}
```

これから書く `PlayerController.cs` は、最初からこの namespace の中に書いていきます。

---

## 2. プレイヤーを動かす「PlayerController」を実装しよう

### 2-1. Rigidbody（物理演算コンポーネント）とは？
`Rigidbody` は、オブジェクトに「重さ」や「速度」といった物理的な性質を与えるコンポーネントです。<br>
これをアタッチしたオブジェクトは、重力で落下したり、他のオブジェクトとぶつかって跳ね返ったりと、Unityの物理エンジンによって動かせるようになります。<br>
プレイヤーの移動も、この `Rigidbody` に「速度」を与えることで実現します。

### 💡 エディタでの作業手順（Playerオブジェクトの準備）
- [ ] `MainGameScene` を開く。
- [ ] Hierarchyに空のオブジェクトを作成し、名前を **`Player`** にする。
- [ ] `Player` に `Rigidbody` コンポーネントを追加する。

### 💡 エディタでの作業手順（スクリプトの作成）
- [ ] `Assets/MyProject/Scripts` フォルダの中に `InGame` フォルダを作成する。
- [ ] `InGame` フォルダの中に **`Player`** フォルダを作成する。
  *※先ほど学んだ namespace の `InGame.Player` に合わせて、フォルダの名前も `Player` にしています。*
- [ ] `Player` フォルダの中に **`PlayerController.cs`** というスクリプトを作成する。
- [ ] 作った `PlayerController.cs` を、Hierarchyの `Player` オブジェクトにドラッグ＆ドロップしてアタッチする。

### 2-2. コードを書こう
以下のコードをコピーして、`PlayerController.cs` に貼り付けてください。1-2で学んだ namespace で、クラス全体を囲んでいることに注目しましょう。

**ファイル名：`PlayerController.cs`**
```cs
using UnityEngine;

namespace TPSRoguelite.InGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        /// <summary>
        /// 移動速度
        /// </summary>
        private const float MOVE_SPEED = 5.0f;

        /// <summary>
        /// 物理演算コンポーネント
        /// </summary>
        [SerializeField] private Rigidbody rigidbody;

        /// <summary>
        /// 移動方向のベクトル
        /// </summary>
        private Vector3 moveDirection;

        /// <summary>
        /// 外部（アニメーションやUIなど）に現在の速度を教えるために保持するVelocity
        /// </summary>
        public Vector3 CurrentVelocity { get; private set; }

        private void Awake()
        {
            if (rigidbody == null)
            {
                Debug.LogError("PlayerにRigidbodyがアタッチされていません！");
            }
        }

        private void Update()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            // 入力値から移動方向のベクトルを作成し、斜め移動が速くならないよう正規化(normalized)する
            moveDirection = new Vector3(x, 0f, z).normalized;
        }

        private void FixedUpdate()
        {
            // 物理演算に関わる移動処理になるため、FixedUpdateで行う
            Move();
        }

        private void Move()
        {
            if (rigidbody == null)
            {
                return;
            }

            // 入力がない場合はピタッと止める
            if (moveDirection == Vector3.zero)
            {
                rigidbody.linearVelocity = new Vector3(0f, rigidbody.linearVelocity.y, 0f);
                CurrentVelocity = Vector3.zero;
                return;
            }

            // 実際の速度を計算
            Vector3 targetVelocity = moveDirection * MOVE_SPEED;

            // Y軸の速度（落下など）は現在の物理演算の値を維持し、XとZのみ上書きする
            rigidbody.linearVelocity = new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.z);

            // 外部（アニメーションやUIなど）に現在の速度を教えるためにプロパティを更新
            CurrentVelocity = rigidbody.linearVelocity;
        }
    }
}
```

- [ ] `Player` の Inspector にある `PlayerController` の `Rigidbody` 欄に、自分自身の `Rigidbody` コンポーネントをドラッグ＆ドロップしてセットする。
- [ ] Playボタンを押し、WASDキー（または矢印キー）でPlayerが動くことを確認する！

### 2-3. なぜ移動処理は `FixedUpdate` で行うの？
`Update` はPCの性能によって呼ばれる間隔がバラバラですが、`FixedUpdate` は常に一定の間隔でキッチリ呼ばれます。<br>
`Rigidbody` を使った物理演算の移動を `Update` で行うと、PCが重くなった瞬間に動きがガクついたり、判定がおかしくなったりするバグの原因になります。そのため、キー入力の「受付」は毎フレーム最速の `Update` で行い、実際に速度を与える「移動」は一定リズムの `FixedUpdate` で行うように役割を分けています。

### 💡 エディタでの作業手順（今日の成果をセーブ）
- [ ] **SourceTree** を開き、変更されたファイルを確認する。
- [ ] `すべてインデックスに追加 (Stage All)` を押す。
- [ ] コミットメッセージに **「PlayerControllerの実装」** と入力する。
- [ ] `コミット` → `プッシュ` の順に押して、クラウドにバックアップする。

---

## 3. 新しい入力システム「Input System」を導入しよう

### 3-1. 今のやり方の弱点
先ほど使った `Input.GetAxisRaw("Horizontal")` は、Unityの昔からある「古い入力方式（Input Manager）」です。<br>
文字列でキーを指定する手軽さはありますが、実は次のような弱点があります。

- **タイプミスに気づけない**：`"Horizontal"` を `"Horizntal"` と打ち間違えても、実行するまでエラーにならない。
- **拡張しにくい**：キーボードだけでなく、ゲームパッドにも対応させたい場合、設定が複雑になりがち。
- **ボタンが押された瞬間を検知しにくい**：「動き続ける入力（移動）」と「一瞬だけ反応してほしい入力（射撃）」を、同じ仕組みで扱っている。

これらを解決するのが、Unity公式の新しいパッケージ **「Input System」** です。<br>
入力の設定を専用のアセットファイルにまとめ、そこから自動生成されたC#のクラス経由でアクセスするため、タイプミスはコンパイルエラーとしてすぐに発見でき、キー割り当ての変更もコードを書き換えずに行えます。

### 💡 エディタでの作業手順（パッケージのインストール）
- [ ] `Window > Package Manager` を開く。
- [ ] 左上のドロップダウンで `Unity Registry` を選択する。
- [ ] 検索欄に `Input System` と入力し、表示されたパッケージの `Install` を押す。
- [ ] 確認ダイアログが出たら `Yes` を選び、エディタが再起動するのを待つ。

### 💡 エディタでの作業手順（Input Actionsアセットの作成）
- [ ] `Assets/MyProject` フォルダの中で右クリック → `Create > Input Actions` を選ぶ。
- [ ] 名前を **`PlayerInputActions`** にする。
- [ ] 作成した `PlayerInputActions` をダブルクリックして設定画面を開く。
- [ ] `Action Maps` 欄の「＋」を押し、名前を **`Player`** にする。
- [ ] `Player` を選んだ状態で `Actions` 欄の「＋」を押し、名前を **`Move`** にする。
  - `Action Type` を `Value`、`Control Type` を `Vector 2` に設定する。
  - `Move` の下の `<No Binding>` を右クリック →`Change Composite Type` → `2D Vector` を選ぶ。
  - 現れた `Up` / `Down` / `Left` / `Right` に、それぞれ `W` / `S` / `A` / `D` キーを割り当てる。
- [ ] もう一つ `Actions` 欄の「＋」を押し、名前を **`Fire`** にする。
  - `Action Type` を `Button` に設定する。
  - `Fire` の下の `<No Binding>` の `Path` に `Mouse > Left Button` を割り当てる。
- [ ] Inspectorで `Generate C# Class` にチェックを入れ、クラス名が `PlayerInputActions` になっていることを確認して `Apply` を押す。
- [ ] 画面上部の `Save Asset` を押して保存する。

これで、`Player.Move`（Vector2の入力）と `Player.Fire`（ボタンが押された）を、コードから安全に読み取れる `PlayerInputActions` というクラスが自動生成されました。

### 3-2. PlayerControllerをInput System対応に書き換えよう
`PlayerController.cs` を、先ほど生成した `PlayerInputActions` を使う形に書き換えます。

**ファイル名：`PlayerController.cs`**
```cs
using UnityEngine;
using UnityEngine.InputSystem;

namespace TPSRoguelite.InGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        /// <summary>
        /// 移動速度
        /// </summary>
        private const float MOVE_SPEED = 5.0f;

        /// <summary>
        /// 物理演算コンポーネント
        /// </summary>
        [SerializeField] private Rigidbody rigidbody;

        /// <summary>
        /// 自動生成されたInputクラス
        /// </summary>
        private PlayerInputActions inputActions;

        /// <summary>
        /// 入力方向
        /// </summary>
        private Vector2 moveInput = Vector2.zero;

        /// <summary>
        /// 外部（アニメーションやUIなど）に現在の速度を教えるために保持するVelocity
        /// </summary>
        public Vector3 CurrentVelocity { get; private set; }

        private void Awake()
        {
            inputActions = new PlayerInputActions();
            inputActions.Player.Fire.performed += OnFire;
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
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        }

        private void FixedUpdate()
        {
            // 物理演算に関わる移動処理になるため、FixedUpdateで行う
            Move();
        }

        private void Move()
        {
            if (rigidbody == null)
            {
                return;
            }

            // 入力がない場合はピタッと止める
            if (moveInput == Vector2.zero)
            {
                rigidbody.linearVelocity = new Vector3(0f, rigidbody.linearVelocity.y, 0f);
                CurrentVelocity = Vector3.zero;
                return;
            }

            // 実際の速度を計算
            Vector3 targetVelocity = new Vector3(moveInput.x, rigidbody.linearVelocity.y, moveInput.y);
            targetVelocity.Normalize();

            rigidbody.linearVelocity = targetVelocity * MOVE_SPEED;

            // 外部（アニメーションやUIなど）に現在の速度を教えるためにプロパティを更新
            CurrentVelocity = rigidbody.linearVelocity;
        }

        private void OnFire(InputAction.CallbackContext context)
        {
            Debug.Log("Fire");
        }
    }
}
```

- [ ] Playボタンを押し、WASDキーでPlayerが動くことを確認する。
- [ ] マウスの左クリックを押し、Consoleウィンドウに `Fire` と表示されることを確認する。

### 3-3. `OnEnable` / `OnDisable` はなぜ必要？
`inputActions` は作っただけでは動きません。`inputActions.Enable()` を呼んで初めて、キーボードやマウスの入力を受け取れるようになります。<br>
そして、オブジェクトが非表示（無効）になったときは `Disable()` で入力の受け取りを止める必要があります。もし止め忘れると、画面に表示されていないオブジェクトが裏でずっと入力を受け取り続け、意図しない動作やメモリの無駄づかいの原因になります。<br>
`OnEnable` / `OnDisable` は、オブジェクトが有効・無効になるたびにUnityが自動で呼んでくれるメソッドなので、ここで入力のON/OFFを管理するのが定番のパターンです。

### 3-4. `ReadValue` と `performed` の違い
コードの中で、`Move` と `Fire` で入力の受け取り方が違うことに気づいたでしょうか。

- **`Move`（`ReadValue<Vector2>()`）**：「今、キーがどれくらい押されているか」という**状態**を、毎フレーム `Update` の中で聞きに行っています。移動のように「押され続けている間、動き続けてほしい」入力に向いています。
- **`Fire`（`performed += OnFire`）**：「ボタンが押された瞬間」という**イベント**を登録しておき、実際に押されたタイミングでUnityが `OnFire` を呼び出してくれます。射撃のように「押した瞬間に1回だけ反応してほしい」入力に向いています。

このように、Input Systemでは入力の性質に合わせて2つの受け取り方を使い分けられるのも便利なポイントです。

### 💡 エディタでの作業手順（今日の成果をセーブ）
- [ ] **SourceTree** を開き、変更されたファイルを確認する。
- [ ] `すべてインデックスに追加 (Stage All)` を押す。
- [ ] コミットメッセージに **「InputSystemの導入」** と入力する。
- [ ] `コミット` → `プッシュ` の順に押して、クラウドにバックアップする。

**これで第2週は完了です！ 次回は、TPS視点の「カメラ制御」と、狙った場所を示す「レーザーポインター」を実装していきます。お楽しみに！**
