# 第6週：オブジェクトプールとイベント駆動

# 本日の目標
1. ゲームのカクつき（処理落ち）の原因である「メモリのゴミ」の仕組みを理解する。
2. 生成と破壊を繰り返さず、一度作ったものを使い回す「オブジェクトプール」を実装する。
3. `UnityAction` を使って、敵と発生装置のプログラムを「安全に連携（イベント駆動）」させる設計を学ぶ。

## 1.ゲームのカクつきを防ぐ「オブジェクトプール」

### 1.1 なぜオブジェクトプールが必要なのか？

シューティングゲームでは、プレイヤーの「弾」や、大量の「敵」が次々と現れては消えていきます。これをUnityのプログラムで、以下のように書いたとします。

・敵が出現するたびに Instantiate（新しく生み出す）<br>
・敵が倒れるたびに Destroy（破壊して消し去る）

一見正しそうですが、実はこの2つの処理は、コンピュータにとって「ものすごく疲れる（重い）作業」なのです。

パソコンのメモリ（作業机）を想像してください。<br>
Instantiateは「新しい書類の束を机にドン！と置く作業」です。<br>
Destroyは「その書類をぐしゃぐしゃに丸めて、机の端にゴミとして捨てる作業」です。

これを毎秒何十回も繰り返すと、机の上は見えないゴミ（不要になったメモリデータ）でいっぱいになります。すると、コンピュータは「これ以上作業できない！一旦ゴミ拾いをするぞ！」とゲームの進行を強制的に止めて、一斉にゴミを片付け始めます。<br>
これを「ガベージコレクション（GC）」と呼びます。

このゴミ拾いが発生した瞬間、皆さんが遊んでいるゲームの画面は「カクッ」と一瞬フリーズします。大量の敵を倒して爽快な瞬間に画面が止まったら、プレイヤーはストレスを感じてしまいますよね。

### 1.2 オブジェクトプール（使い回し）の仕組み
そこで登場するのが「オブジェクトプール（Object Pool）」というプロのテクニックです。<br>
直訳すると「モノの溜め池」です。

考え方はとてもエコでシンプルです。
1. ゲームが始まる前のロード画面などで、あらかじめ敵や弾を「50個」くらい作っておく（Instantiate）
2. 作ったものは、一旦すべて「非表示（SetActive(false)）」にして、見えない裏で待機させておく
3. 敵を出したい時は、新しく作るのではなく、待機している敵を「表示（SetActive(true)）」にして使い回す
4. 敵が倒された時も、破壊（Destroy）するのではなく、再び「非表示」に戻して裏の待機列に並ばせる
   
この仕組みなら、ゲームの最中に Instantiate も Destroy も一度も呼ばれません。つまり、メモリのゴミが一切出ないので、どんなに激しい弾幕や大量の敵を出しても、ゲームが全くカクつかずに動くようになります。

レストランのお皿に例えるとわかりやすいです。<br>
お客さんが来るたびに「紙皿（Instantiate）」を買ってきて、食べ終わったら「捨てる（Destroy）」のは非効率でお金（処理）がかかります。<br>
最初から「陶器のお皿（オブジェクトプール）」をたくさん用意しておき、洗い場と客席でぐるぐると「使い回す」のが、ゲーム開発で大切なことです。

### 1.3 実践：敵の生成処理を「使い回し」に大改造しよう！
ここまで皆さんが作ったゲームは、敵が出るたびに「Instantiate（紙皿を買う）」、倒すたびに「Destroy（捨てる）」をしていました。これを「オブジェクトプール（陶器のお皿を洗って使い回す）」方式に改修（リファクタリング）します。<br>
書き換えるのは「敵自身（EnemyState）」と「発生装置（EnemySpawner）」の2つです。
また、「UnityAction（連絡先の受け渡し）」という高度なテクニックに挑戦します。
■ なぜ「連絡先」が必要なの？
敵は自分が倒れた時、発生装置に対して「倒れたからプール（列）に戻して！」とお願いしなければなりません。
しかし、敵のプログラムの中に「発生装置を探して命令する」処理を書いてしまうと、その敵は発生装置がないステージではエラーを起こす「使い回せない不便な部品」になってしまいます。

そこで、敵が作られた瞬間に、発生装置から「もし倒れたら、この電話番号（Action）に連絡してね」と連絡先だけを渡しておきます。敵は自分がどこにいるか気にせず、ただ「倒れました」と連絡するだけで済む設計になります。

1. 敵自身の改修（連絡先を受け取り、倒れたら連絡する）

敵のHPが0になったとき、Destroy するのではなく「非表示（SetActive(false))」にして、再び出番が来るまで待機させるように書き換えます。<br>
**ファイル名： `EnemyState.cs` （コードを改修）**
``` diff
using UnityEngine;
+ using UnityEngine.Events; // UnityAction（連絡先）を使うために必要

namespace TPSRoguelite.InGame.Enemy
{
    public class EnemyState : MonoBehaviour, IDamageable
    {
        public int CurrentHp { get; private set; }
        private const int MAX_HP = 100;

+       public event UnityAction<GameObject> OnReturnToPoolAction;

+       private void OnEnable()
+       {
+           // オブジェクトプールで再利用される時、表示された瞬間にHPを元に戻す
+           CurrentHp = MAX_HP;
+       }

        public void TakeDamage(int damageAmount)
        {
            if (damageAmount <= 0) return;

            CurrentHp -= damageAmount;

            if (CurrentHp <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
-           Destroy(gameObject); 破壊しないように、この処理を削除！！
+           gameObject.SetActive(false); // 死んだふりをして裏に下がる
+
+           // 倒れたら、貰っていた連絡先に自分自身を渡して報告する
+           // 「?.」は、もし連絡先が空っぽじゃなければ実行する、という安全な書き方です
+           OnReturnToPoolAction?.Invoke(gameObject);
        }   
    } 
}
```

2. 発生装置の改修（敵に連絡先を渡し、呼ばれたらプールに戻す）<br>
「Queue（キュー）」という順番待ちの列を作り、敵を作った時に「ReturnToPool」という自分自身のメソッド（連絡先）を敵に渡してあげます。<br>
**ファイル名： `EnemyState.cs` （コードを改修）**
``` diff
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
+ using System.Collections.Generic; // Queueを使うために必要

namespace TPSRoguelite.InGame.Spawner
{
    public class EnemySpawner : MonoBehaviour
    {
        private const float SPAWN_INTERVAL = 3.0f;

+       /// <summary>
+       /// 最初に用意する敵の数
+       /// </summary>
+       private const int POOL_SIZE = 20;

+       /// <summary>
+       /// 敵を待機させておく順番待ちの列（プール）
+       /// </summary>
+       private Queue<GameObject> enemyPool;

+       private void Awake()
+       {
+           // プールの初期化
+           enemyPool = new Queue<GameObject>();
+           
+           if (enemyPrefab == null)
+           {
+               return;
+           }
+   
+           // ゲーム開始時に、あらかじめPOOL_SIZEの数だけ敵を作って非表示で並ばせておく
+           for (int i = 0; i < POOL_SIZE; i++)
+           {
+               GameObject enemy = Instantiate(enemyPrefab);
+               enemy.SetActive(false); // 見えないようにする
+               enemyPool.Enqueue(enemy); // 列に並ばせる（Enqueue）
+           }
+       }

        private void Start()
        {
            SpawnLoopAsync().Forget();
        }

        private async UniTaskVoid SpawnLoopAsync()
        {
            var token = this.GetCancellationTokenOnDestroy();
            while (true)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(SPAWN_INTERVAL), cancellationToken: token);
+               SpawnEnemyFromPool();
-               SpawnEnemy();
            }
        }

        /// <summary>
        /// 敵の生成
        /// </summary>
+       private void SpawnEnemyFromPool()
-       private void SpawnEnemy()
        {
            if (enemyPrefab == null || spawnPoints.Length == 0)
            {
                return;
            }

            int randomIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomIndex];
  
            Vector3 safePosition = spawnPoint.position;
            if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, MAX_SPAWN_DISTANCE, NavMesh.AllAreas))
            {
                safePosition = hit.position;
            }
            else
            {
                Debug.LogWarning("近くに安全なスポーン位置が見つかりませんでした。");
                return;
            }

-           GameObject enemy = Instantiate(enemyPrefab, safePosition, spawnPoint.rotation);
-           Debug.Log("敵を生成(Instantiate)しました！");
    
+           // プールから敵を取り出す処理
+           GameObject spawnEnemy = null;
+   
+           // 列に待機している敵がいれば、それを取り出す（Dequeue）
+           if (enemyPool.Count > 0)
+           {
+               spawnEnemy = enemyPool.Dequeue();
+           }
+           else
+           {
+               // もし列が空っぽ（全員出撃中）なら、仕方ないので新しく作る
+               spawnEnemy = Instantiate(enemyPrefab);
+               Debug.LogWarning("プールに空きがなたったため、敵を生成(Instantiate)しました。POOL_SIZEを調整するか、生成数を制限してください。");
+           }
+
+           // 敵が出撃する瞬間に「倒れたらReturnToPoolを実行してね」とイベントを登録（+=）する
+           EnemyState enemyState = spawnEnemy.GetComponent<EnemyState>();
+           if (enemyState != null)
+           {
+               // 安全のため、一度解除してから登録し直すことで、二重登録のバグを防ぐ
+               enemyState.OnReturnToPoolAction -= ReturnToPool;
+               enemyState.OnReturnToPoolAction += ReturnToPool;
+           }
+
+           // 敵の配置と表示
+           int randomIndex = Random.Range(0, spawnPoints.Length);
+           Transform spawnPoint = spawnPoints[randomIndex];
+   
+           spawnEnemy.transform.position = safePosition;
+           spawnEnemy.transform.rotation = spawnPoint.rotation;
+   
+           spawnEnemy.SetActive(true);
        }

+       /// <summary>
+       /// プールへ戻す
+       /// </summary>
+       private void ReturnToPool(GameObject enemy)
+       {
+           enemyPool.Enqueue(enemy);
+
+           // イベントの登録を解除（-=）する
+           EnemyState enemyState = enemy.GetComponent<EnemyState>();
+           if (enemyState != null)
+           {
+               enemyState.OnReturnToPoolAction -= ReturnToPool;
+           }
+       }
    }
}
```

これで、裏側で20体の敵がぐるぐると「使い回される」最強のエコシステムが完成しました！ゲームを再生しながらHierarchyウィンドウを見てください。<br>
敵が新しく作られず、グレー（非表示）と白（表示）が切り替わっているだけなのが確認できるはずです。
