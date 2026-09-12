# 第4週：射撃・カプセル化・リロード処理
1. 「カプセル化（インターフェースとアクセス修飾子）」を学び、安全な設計を身につける。
2. 画面中央に向けて弾を撃ち（Raycast）、敵にダメージを与える。
3. `UniTask` を用いて、非同期でのリロード処理（状態管理）を実装する。

---

##  1. インターフェースの実装

### 1-1. インターフェースと敵の作成（新規）

1. Scriptフォルダに `Core` という名前のフォルダを追加しましょう。
2. 作成した `Core` に `Interface` という名前のフォルダを追加しましょう。
3. `Interface` フォルダの中に `IDamageable.cs` という名前のスクリプトを作成します。

**ファイル名: `IDamageable.cs`**
``` cs
namespace Core.Interface
{
    public interface IDamageable
    {
        /// <summary>
        /// ダメージを与える
        /// </summary>
        public void TakeDamage(int damageAmount);
    }
}
```

1. ScriptフォルダにあるInGameフォルダに `Enemy` という名前のフォルダを追加しましょう。
2. Cameraフォルダの中に `EnemyState.cs` という名前のスクリプトを作成します。
   
**ファイル名: `EnemyState.cs`**
``` cs
using UnityEngine;
using Core.Interface;

namespace TPSRoguelite.InGame.Enemy
{
    public class EnemyState : MonoBehaviour, IDamageable
    {
        /// <summary>
        /// 体力の最大値
        /// </summary>
        private const int MAX_HP = 100;

        /// <summary>
        /// 現在の体力
        /// </summary>
        public int CurrentHp { get; private set; }

        private void Awake()
        {
            CurrentHp = MAX_HP;
        }

        public void TakeDamage(int damageAmount)
        {
            // マイナスのダメージ（回復）を防ぐ
            if (damageAmount <= 0)
            {
                return;
            }

            CurrentHp -= damageAmount;
            Debug.Log($"敵に {damageAmount} のダメージ！ 残りHP: {CurrentHp}");

            if (CurrentHp <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("敵を倒しました。");
            Destroy(gameObject);
        }
    }
}
```

### 1-2. 敵を倒す
PlayerController.csの中身を改修しましょう。

** ファイル名：`PlayerController.cs` **
```diff
        private const float MOVE_SPEED = 5.0f;
        private const float ROTATION_SPEED = 10.0f;

+       /// <summary>
+       /// 相手に与えるダメージ量
+       /// </summary>
+       private const int ATTACK_DAMAGE = 20;
+
+       /// <summary>
+       /// 攻撃距離（射撃範囲）
+       /// </summary>
+       private const float ATTACK_RANGE = 50f;

        // (既存のメンバ変数は省略)

        private void OnFire(InputAction.CallbackContext context)
        {
-           Debug.Log("Fireボタンが押されました。");
+           // カメラの中央から真っ直ぐ前へ光線を飛ばす
+           Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);
+
+           // 光線が何かに当たったか判定
+           if (Physics.Raycast(ray, out RaycastHit hitInfo, ATTACK_RANGE))
+           {
+               Debug.Log($"{hitInfo.collider.name} に命中！");
+
+               // 当たった相手が IDamageable (ダメージを受けられる性質) を持っているか確認
+               IDamageable target = hitInfo.collider.GetComponent<IDamageable>();
+
+               // ダメージを受けられる性質を持っていればダメージ処理を行う
+               if (target != null)
+               {
+                   target.TakeDamage(ATTACK_DAMAGE);
+               }
+           }
+        }

// (Update, FixedUpdate, Move 等は省略)
```

### 1-3. インターフェース（Interface）って何？
**■ インターフェースは「絶対に守るべきルールブック」**<br>
「インターフェース（Interface）」は、直訳すると「接点」や「境界面」という意味ですが、プログラミングの世界では「このルール（メソッド）を絶対に持ってくださいね！」という「ルールブック（契約書）」のことです。<br>

例えば、今回作った `IDamageable`（ダメージを受けられるもの）というルールブックには、「TakeDamage（ダメージを受ける）というメソッドを必ず作ること」とだけ書かれています。中身（どうやってHPを減らすか）は書いてありません。<br>

このルールブックにサイン（実装）したクラス（今回はEnemyState）は、絶対に `TakeDamage` を自分のコードの中に書かなければいけません。もし書き忘れると、Unityの再生ボタンを押す前にC#が「ルール違反です！」と赤いエラーを出して教えてくれます。<br>

**■ もしインターフェースがなかったら？**<br>
なぜわざわざそんなルールブックを作るのでしょうか？<br>
例えば、皆さんが作ったシューティングゲームがどんどん大きくなって、敵のロボット（Enemy）、爆発するドラム缶（DrumCan）、味方を守るシールド（Shield）など、色々なものを撃てるようになったとします。

もしインターフェースを使わずに、プレイヤーの射撃プログラムを書くとこうなります。
``` cs
if (当たった相手が Enemy なら)
{
    相手.EnemyのHPを減らす(); 
}
else if (当たった相手が DrumCan なら)
{
    相手.DrumCanを爆発させる();
}
else if (当たった相手が Shield なら) 
{ 
    相手.Shieldにヒビを入れる();
}
```

これでは、新しい敵やオブジェクトを追加するたびに、プレイヤーの「撃つ」プログラムを延々と書き足さなければなりません。バグの温床（if文地獄）になります。

**■ インターフェースの魔法：「相手が誰でも気にしない」**<br>
ここで `IDamageable` というインターフェースの登場です。<br>
ロボットも、ドラム缶も、シールドも、すべて `IDamageable` というルールブックにサインさせます。

すると、プレイヤーの射撃プログラムは、たったこれだけで済むようになります。
``` cs
if (当たった相手が IDamageable（ルールブックにサインしている奴）なら)
{
    相手.TakeDamage(20); // 種類は気にしない！とりあえずダメージを受け取れ！
}
```

プレイヤーの弾は、「当たった相手がロボットかドラム缶かはどうでもいい。TakeDamageというメソッドを持っていることだけはルールブックで保証されているから、とりあえずそれを実行する！」という考え方をします。<br>
これを難しい言葉で「ポリモーフィズム（多様性）」と呼びます。

**■ 身近な例え：「USB端子」**<br>
皆さんの身近にある「USB」もインターフェースの代表例です。<br>
パソコン（プレイヤーの弾）は、USBポートに刺さったものがマウスなのか、キーボードなのか、扇風機なのかを気にしません。<br>
「USBという形のルール（インターフェース）を守っているなら、とりあえず電気を流すよ！」という仕組みですよね。<br>

プログラミングのインターフェースも全く同じです。<br>
「IDamageableというルールを守っているなら、とりあえずダメージを与えるよ！」という接点（USBポート）を作っているのです。

これを使いこなせるようになると、プログラム同士が複雑に絡み合うのを防ぎ、いくらでもゲームを拡張できるようになります。

**■ インターフェースじゃなくて「継承」じゃダメなの？**<br>
敵（EnemyBase）という親クラスを作って、そこに TakeDamage を書けばいいんじゃないの？」と思った人もいるかもしれません。<br>
たしかに継承を使っても同じようなことはできます。しかし、インターフェースを選ぶのには、理由が2つあります。<br>

理由1：親クラスが「何でも屋」になってパンクする<br>
もし継承だけで解決しようとして「ダメージを受けるもの（DamageableBase）」という巨大な親クラスを作ったとします。<br>
最初は「敵」だけでしたが、後から「ドラム缶」「ガラス」「味方のシールド」など、色々なものを壊せるようにしたくなりました。<br>
すると、この親クラスの中に「敵がやられた時の処理」「ドラム缶が爆発する処理」「ガラスが割れる音」などが全て詰め込まれ、数千行の地獄のようなプログラムになってしまいます。これを「神クラス（God Class）化」と呼び、バグの温床になりやすい現象です。

理由2：C#のルール「親は1人しか選べない、でも資格はいくつでも取れる」<br>
オブジェクト指向の考え方として、継承とインターフェースは根本的に意味が違います。<br>

・継承 ＝「血縁関係（〜は〜の子供である）」<br>
・インターフェース ＝「資格・ライセンス（〜ができる）」<br>

例えば、「スライム」は「敵」の子供なので継承を使うのが自然です。<br>
しかし、「ドラム缶」は敵の子供ではありませんよね。<br>

さらに、C#という言語には「親クラスは1つしか継承できない」という絶対ルールがあります。<br>
もしドラム缶に、「壊れる（IDamageable）」「燃える（IIgnitable）」「プレイヤーが持ち運べる（IPickupable）」という3つの特徴を持たせたい場合、継承だと親を1人しか選べないのでプログラミングができなくなってしまいます。<br>

しかしインターフェースは「資格」なので、1つのクラスにいくつでも持たせることができます。<br>
「ドラム缶クラスは、壊れる資格と、燃える資格と、持ち運べる資格を持っています！」と宣言するだけで、それぞれのルールを綺麗に組み合わせることができるのです。<br>

だからこそ、「ダメージを受ける」というような「色々な物体に共通して持たせたい能力」には、継承ではなくインターフェースを使うのです。

---
## 2. リロード処理

### 2-1. リロード用のボタン「Reload」を追加しよう
リロードのプログラムを書く前に、プレイヤーがどのキーを押したらリロードするのかをUnityに教える設定が必要です。<br>
シューティングゲームで定番の「R」キーを割り当ててみましょう。
1. 入力設定ファイル「PlayerInputActions」をダブルクリックして開きます。
2. Actionsのリストの右にある「＋」ボタンを押し、新しいアクションを作り、名前を「Reload」にします。
3. 作った「Reload」を選択し、右側の Action Type が「Button」になっていることを確認します。
4. Reloadの下にある「」を選択し、右側の Path をクリックして「Keyboard」の中にある「R」を選びます。
5. 設定画面の右上、または左上にある「Save Asset」ボタンを忘れずに押して保存し、画面を閉じます。

### 2-2. UniTaskの導入手順
UniTaskはUnityの標準機能ではないため、外部からパッケージ（拡張機能）としてインストールする必要があります。以下の手順でプロジェクトに導入しましょう。

**手順1：パッケージマネージャーを開く**<br>
Unityの上のメニューバーから「Window」＞「Package Manager」をクリックして開きます。<br>

**手順2：GitのURLを入力する画面を出す**<br>
Package Managerの左上にある「＋」の形をしたボタンをクリックし、開いたメニューから「Add package from git URL...」を選択します。

**手順3：URLを入力してインストール**<br>
入力欄が表示されるので、以下のURLをコピーして貼り付けます。<br>

https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask

貼り付けたら「Add」ボタンを押します。<br>
緑色のゲージが進み、インストールが完了するまでしばらく待ちます（数十秒〜数分かかります）。<br>

**手順4：導入の確認**<br>
インストールが終わると、Package Managerのリストに「UniTask」という項目が追加されます。<br>
これで、スクリプトの一番上に`using Cysharp.Threading.Tasks;`と書くことで、非同期処理が使えるようになります。

### 2-3. UniTaskを用いてリロード処理を実装する
** ファイル名：`PlayerController.cs` **
```diff
  using UnityEngine;
  using UnityEngine.InputSystem;
+ using Cysharp.Threading.Tasks;

  public class PlayerController : MonoBehaviour
  {
      private const float MOVE_SPEED = 5.0f;
      private const float ROTATION_SPEED = 10.0f;
      private const float LASER_MAX_DISTANCE = 50.0f;
+     private const int ATTACK_DAMAGE = 20;
+     private const float ATTACK_RANGE = 50.0f;
+     private const int MAX_AMMO = 30;
+     private const float RELOAD_TIME = 1.5f;

      // (既存のメンバ変数は省略)
+     private bool isReloading;

      private void Awake()
      {
          // (既存の初期化処理は省略)

+         CurrentAmmo = MAX_AMMO;
+         inputActions.Player.Fire.performed += OnFire;
+         inputActions.Player.Reload.performed += OnReload;
      }

+     private void OnFire(InputAction.CallbackContext context)
+     {
+         if (isReloading || CurrentAmmo <= 0)
+         {
+             Debug.Log("弾切れ、またはリロード中です！");
+             return; 
+         }
+ 
+         CurrentAmmo--;
+         Debug.Log($"発砲！ 残り弾数: {CurrentAmmo}");
+ 
          Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);
          if (Physics.Raycast(ray, out RaycastHit hitInfo, ATTACK_RANGE))
          {
              IDamageable target = hitInfo.collider.GetComponent<IDamageable>();
              if (target != null)
              {
                  target.TakeDamage(ATTACK_DAMAGE);
              }
          }
      }
 
+     private void OnReload(InputAction.CallbackContext context)
+     {
+         if (isReloading || CurrentAmmo == MAX_AMMO)
+         {
+             return;
+         }
+
+         ReloadAsync().Forget();
+     }
+ 
+     private async UniTask ReloadAsync()
+     {
+         isReloading = true;
+         Debug.Log("リロード開始...");
+ 
+         await UniTask.Delay(System.TimeSpan.FromSeconds(RELOAD_TIME), cancellationToken: this.GetCancellationTokenOnDestroy());
+ 
+         CurrentAmmo = MAX_AMMO;
+         isReloading = false;
+         Debug.Log("リロード完了！");
+     }

      // (Update, FixedUpdate, Move 等は省略)
  }
```


### 2-2. 非同期処理「UniTask」とは？
シューティングゲームで弾を撃ち尽くしたとき、「1.5秒待ってから、弾を最大まで回復させる」といった時間待ちの処理が必要になります。<br>
Unityで「時間を待つ」方法には、大きく分けて「コルーチン」と「UniTask（ユニタスク）」の2種類があります。<br>

**■ 昔の方法：コルーチン（Coroutine）**<br>
Unityの標準機能にある「待つ」ための仕組みです。<br>
もし今回のリロード処理をコルーチンで書くと、以下のようになります。（記載不要です）<br>
```cs
private void OnReload()
{
    StartCoroutine(ReloadCoroutine()); // 呼び出し方が少し特殊
}

private IEnumerator ReloadCoroutine() // 戻り値がIEnumeratorという特殊な型
{
    isReloading = true;
    Debug.Log("リロード開始...");

    yield return new WaitForSeconds(1.5f); // 待機するための特殊な呪文

    CurrentAmmo = 30;
    isReloading = false;
    Debug.Log("リロード完了！");
}
```

・弱点1：実行するたびに「メモリのゴミ」が出る。<br>
コルーチンは使うたびに、コンピュータのメモリ内に見えないゴミを残します。<br>
ゴミが溜まると、コンピュータが一斉にゴミ拾い（ガベージコレクション）を始め、その瞬間にゲームの画面が一瞬カクッと止まってしまいます。<br>

・弱点2：書き方が少し複雑。<br>
エラーが起きたときの処理などを組み込みにくく、複雑なゲームを作ろうとするとコードがごちゃごちゃになりがちです。<br>

**■ 今の方法：UniTask**<br>
多くのエンジニアが使用している拡張機能で、C#という言語が元々持っている「async/await（エイシンク/アウェイト）」という最新の仕組みを、Unityで爆速で動くように改良したものです。<br>

・強み1：メモリのゴミを一切出さない（ゼロアロケーション）<br>
何度リロード処理を繰り返してもゴミが出ないため、大量の敵が出現するゲームでも画面がカクつきません。<br>

・強み2：上から下へ、素直にコードが読める。<br>
`await UniTask.Delay(1.5秒);`と書くだけで、そこでピタッと処理が一時停止し、1.5秒後にその下の行から処理が再開されます。<br>
直感的でバグが起きにくいのが特徴です。<br>

・注意点
UniTaskは超高速で便利ですが、1つだけ恐ろしい落とし穴があります。
それは「自分がくっついているオブジェクトが破壊（Destroy）されても、裏でずっと動き続けてしまう」という性質です。

もし、敵が「10秒後に大爆発するUniTask」を起動したとします。
しかし、3秒後にプレイヤーがその敵を撃って破壊（Destroy）しました。
敵は画面から消えましたが、裏で動いていたUniTaskは消えません。そして10秒経ったとき、「よし、大爆発だ！」と消えたはずの敵を爆発させようとして、ゲームが進行不能になるエラーを引き起こします。
その点、コルーチンは、自分がくっついているオブジェクトが破壊されたり、非表示（SetActive(false)）になったりすると、「自動的に」タイマーがストップして消滅してくれます。

これを防ぐためには、「もしこのオブジェクトが破壊されたら、このタイマーもストップしてね」という「キャンセルの切符」を渡してあげる必要があります。

これを「キャンセルトークン（CancellationToken）」と呼びます。
書き方はとても簡単で、Delayのカッコの中に`this.GetCancellationTokenOnDestroy()`を足すだけです。

### 2-EXTRA ★チャレンジ課題（任意）
`OnFire` メソッドの中で、`CurrentAmmo` が 0 になった瞬間に、`自動的に ReloadAsync().Forget();` を呼び出す「オートリロード機能」を追加してみよう。
