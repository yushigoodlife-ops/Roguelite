using UnityEngine;
using UnityEngine.AI;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;

namespace TPSRoguelite.InGame.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        private const string PLAYER_TAG_NAME = "Player";

        /// <summary>
        /// ノックバックする強度
        /// </summary>
        private const float KNOCKBACK_FORCE = 2.0f;

        /// <summary>
        /// ノックバックする時間
        /// </summary>
        private const float KNOCKBACK_DURARION = 0.15f;

        /// <summary>
        /// 敵の本体
        /// </summary>
        [SerializeField] private EnemyState enemyState = null;

        /// <summary>
        /// NavMeshAgent
        /// </summary>
        [SerializeField] private NavMeshAgent navMeshAgent = null;

        /// <summary>
        /// 目的地となるPlayerのTransform
        /// </summary>
        private Transform targetPlayer = null;

        /// <summary>
        /// ノックバック動作のキャンセルトークン
        /// </summary>
        private CancellationTokenSource hitCts;

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

            if (navMeshAgent != null && enemyState != null && enemyState.EnemyDataAsset != null)
            {
                navMeshAgent.speed = enemyState.EnemyDataAsset.MoveSpeed;
            }
        }

        private void Update()
        {
            // ターゲット（プレイヤー）とナビが存在しているか
            if (targetPlayer != null && navMeshAgent != null) 
            {
                // プレイヤーの現在位置を毎フレーム目的地として設定する
                navMeshAgent.SetDestination(targetPlayer.position);
            }
        }

        private void OnEnable()
        {
            if (enemyState != null)
            {
                enemyState.OnDamageAction -= HandleDamage;
                enemyState.OnDamageAction += HandleDamage;
            }
        }

        private void OnDisable()
        {
            if (enemyState != null)
            {
                enemyState.OnDamageAction -= HandleDamage;
            }

            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.isStopped = false;
            }
        }

        /// <summary>
        /// ノックバック
        /// </summary>
        private async UniTaskVoid KnockbackAsync(CancellationToken token)
        {
            if (navMeshAgent == null)
            {
                return;
            }

            bool wasStopped = navMeshAgent.isStopped;
            navMeshAgent.isStopped = true;

            if (targetPlayer != null)
            {
                Vector3 dir = (transform.position - targetPlayer.position).normalized;
                dir.y = 0;
                transform.position += dir * KNOCKBACK_FORCE;
            }

            bool isCanceled = await UniTask.Delay(
                TimeSpan.FromSeconds(KNOCKBACK_DURARION), cancellationToken: token)
                .SuppressCancellationThrow();

            if (!isCanceled && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.isStopped = wasStopped;
            }
        }

        private void HandleDamage()
        {
            hitCts?.Cancel();
            hitCts?.Dispose();
            hitCts = null;

            hitCts = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                hitCts.Token, this.GetCancellationTokenOnDestroy());

            KnockbackAsync(linkedCts.Token).Forget();
        }
    }
}
