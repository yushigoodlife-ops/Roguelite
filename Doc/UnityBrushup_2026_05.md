# 第5週：敵の追跡AI（NavMesh）をマスターしよう

# 本日の目標
1. 「AI Navigation」を使って、ステージ上に敵が歩ける道（NavMesh）を作る。
2. プログラムで敵にカーナビを付け、自動でプレイヤーを追いかけるAIを実装する。
3. `NavMesh.SamplePosition` と `UniTask` を活用し、安全な場所に敵を自動生成（スポーン）させる仕組みを作る。

## 1.敵の通り道を作る：NavMeshの基礎と設定

### 1.1 自動で障害物を避けるAIの仕組み「NavMesh」とは？
敵キャラクターがプレイヤーを追いかけてくるとき、ただ真っ直ぐ向かってくるだけでは、壁にぶつかってずっと足踏みをしてしまいます。壁を避け、階段を上り、賢くプレイヤーの場所まで「道案内」をしてくれるUnityの機能が「NavMesh（Navigation Mesh）」です。

例えるなら、NavMeshは「見えない道路地図」、そして敵にくっつけるコンポーネント（NavMesh Agent）は「賢いカーナビ」です。この2つが揃うことで、敵は自動的に障害物を避けて目的地へ進んでくれます。

### 1.2 コンポーネントで簡単ベイク！「AI Navigation」による道作り
Unityの新しいバージョンでは、「コンポーネント（部品）」をくっつけるだけで簡単に道が作れるようになりました。

手順1：パッケージのインストール<br>
画面上のメニューから「Window」＞「Package Manager」を開きます。左上の「Packages:」を「Unity Registry」に変更し、リストから「AI Navigation」を探して、右上の「Install」ボタンを押します。（※インストール済みの場合は不要です）

手順2：障害物（壁）を配置する<br>
床に道を作る前に、敵の邪魔になる「壁」を作りましょう。Hierarchyで右クリックし、「3D Object」＞「Cube」などを作成して、床の上にいくつか配置します。あとで道を作る際、システムが自動的にこの壁を読み取って「ここは歩けない」と判断してくれます。

手順3：床となるオブジェクトに部品をつける<br>
Hierarchyで、ステージの「床」となるオブジェクトを選択します。Inspectorの一番下にある「Add Component」ボタンを押し、「NavMeshSurface」を追加します。

手順4：道を作る（Bake）<br>
追加した NavMeshSurface コンポーネントの中に、「Bake（焼き付け）」というボタンがあるので押します。すると、ステージの床に「青いモヤモヤ」が表示されます。先ほど置いた「壁」の周りだけモヤモヤがくり抜かれていれば、自動計算は成功です！

手順5：敵にカーナビを付ける<br>
敵のオブジェクトを選択し、Add Componentから「NavMesh Agent」を追加します。あとはプログラムから目的地を教えるだけで、敵は壁を賢く迂回して追いかけてきます！

### 1.3 標的を捕捉せよ：敵がプレイヤーを追いかけるAIスクリプトの実装
ナビと道路が完成したので、実際に敵がプレイヤーを見つけて追いかける「AI（人工知能）」のスクリプトを作成します。

手順1：プレイヤーの準備
Hierarchyでプレイヤーのオブジェクトを選択し、Inspectorの一番上にある「Tag」を「Player」に変更します。これで、敵がプレイヤーを見つけやすくなります。

手順2：スクリプトの作成とアタッチ
以下のスクリプトを作成し、敵のプレハブ（またはオブジェクト）にアタッチします。

**ファイル名： `EnemyController.cs`**
```cs
using UnityEngine;
using UnityEngine.AI; // NavMeshAgentを使うために必要

namespace TPSRoguelite.InGame.Enemy
{  
    public class EnemyController : MonoBehaviour
    {
        private NavMeshAgent navMeshAgent;
        private Transform targetPlayer;

        private void Awake()
        {
            // 自分のくっついているNavMeshAgentコンポーネントを取得
            navMeshAgent = GetComponent<NavMeshAgent>();

            // "Player"というタグがついたオブジェクトを探し出す
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                targetPlayer = player.transform;
            }
            else
            {
                Debug.LogError("Playerタグのついたオブジェクトが見つかりません！");
            }
        }

        private void Update()
        {
            // ターゲット（プレイヤー）とナビが存在している場合のみ実行
            if (targetPlayer != null && navMeshAgent != null)
            {
                // 毎フレーム、プレイヤーの現在位置を「目的地」としてカーナビにセットする
                navMeshAgent.SetDestination(targetPlayer.position);
            }
        }
    }
}
```

ゲームを再生してみましょう。敵が障害物をスルスルと避けて、プレイヤーに向かって歩いてくればAIの完成です！

## 2.敵の自動生成システム：安全な場所へのスポーン制御

### 2.1 壁の中への出現を防ぐ！SamplePositionによる安全な座標探し
ゲーム中、敵をランダムな場所から出現（スポーン）させたい時があります。
しかし、ただ座標をランダムに決めるだけでは、敵が「壁の中」や「空の彼方（NavMeshがない場所）」に出現してしまい、エラーになって動けなくなるバグがよく発生します。

そこで、「SamplePosition（サンプルポジション）」という機能を使います。<br>
これは、「ランダムに決めた出現座標の近くに、ちゃんと青いモヤモヤ（NavMesh）があるかな？」と探りを入れる機能です。<br>
もし近くに歩ける場所があればそこに出現させ、なければ別の場所を探し直すというプログラムを書くことで、絶対に壁に埋まらない安全な敵の出現システムを作ることができます。

### 2.2 UniTaskを用いた非同期ループによる、敵の自動スポーン処理の実装
手順1：Hierarchyで空のオブジェクトを作り、名前を「EnemySpawner」にします。<br>
手順2：以下のスクリプトを作成し、EnemySpawnerにアタッチします。<br>
**ファイル名： `EnemySpawner.cs`**
``` cs
using UnityEngine;
using UnityEngine.AI; // NavMeshを使うために必要
using Cysharp.Threading.Tasks; // UniTaskを使うために必要

namespace TPSRoguelite.InGame.Spawner
{    
    public class EnemySpawner : MonoBehaviour
    {
        /// <summary>
        /// 出現時間
        /// </summary>
        private const float SPAWN_INTERVAL = 3.0f;

        /// <summary>
        /// 道を探す最大距離
        /// </summary>
        private const float MAX_SPAWN_DISTANCE = 2.0f;

        /// <summary>
        /// 敵のプレハブ
        /// </summary>
        [SerializeField] private GameObject enemyPrefab;

        /// <summary>
        /// 出現ポイント
        /// </summary>
        [SerializeField] private Transform[] spawnPoints;

        private void Start()
        {
            SpawnLoopAsync().Forget();
        }

        /// <summary>
        /// UniTaskを用いた非同期の生成ループ
        /// </summary>
        private async UniTaskVoid SpawnLoopAsync()
        {
            // 発生装置が壊された時にタイマーを安全に止めるための切符（トークン）を取得
            var token = this.GetCancellationTokenOnDestroy();

            // 無限ループ（awaitがあるためフリーズしません）
            while(true)
            {
                // 指定時間待機する
                await UniTask.Delay(System.TimeSpan.FromSeconds(SPAWN_INTERVAL), cancellationToken: token);
                SpawnEnemy();
            }
        }

        /// <summary>
        /// 敵の生成
        /// </summary>
        private void SpawnEnemy()
        {
            if (enemyPrefab == null || spawnPoints.Length == 0)
            {
                return;
            }

            // ランダムな出現場所を選ぶ
            int randomIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomIndex];

            // --- 安全な座標を探す ---
            Vector3 safePosition = spawnPoint.position;

            // 選んだポイントの周囲にNavMesh（歩ける道）があるか探す
            if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, MAX_SPAWN_DISTANCE, NavMesh.AllAreas))
            {
                // 見つかったら、その安全な座標で上書きする
                safePosition = hit.position;
            }
            else
            {
                // 見つからなければ今回は生成を諦めてスキップする
                Debug.LogWarning("近くに安全なスポーン位置が見つかりませんでした。");
                return;
            }

            // 敵のクローンを生成する
            GameObject enemy = Instantiate(enemyPrefab, safePosition, spawnPoint.rotation);
            Debug.Log("敵を生成(Instantiate)しました！");
        }
    }
}
```

これで、3秒ごとに敵が現れ、プレイヤーを追いかけてくるようになりました。<br>
※弾を当てると、EnemyState.cs の機能で Destroy されて消えます。
