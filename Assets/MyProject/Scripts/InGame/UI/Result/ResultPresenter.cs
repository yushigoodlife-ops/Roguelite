using UnityEngine;
using UnityEngine.SceneManagement;
using TPSRoguelite.InGame.Manager;

namespace TPSRoguelite.UI
{
    public class ResultPresenter : MonoBehaviour
    {
        private const string TITLE_SCENE_NAME = "TitleScene";
        private const string IN_GAME_SCENE_NAME = "InGameScene";

        [SerializeField] private ResultView resultView;
        private ResultModel resultModel;

        private void Start()
        {
            if (resultView == null) {
                return;
            }

            resultModel = new ResultModel();
            resultModel.Initialize();

            resultView.OnRetryAction += RetryGame;
            resultView.OnReturnToTitleAction += ReturnToTitle;

            string message = string.Empty;
            if (resultModel.IsClear) {
                message = $"GAME CLEAR!\n\n到達レベル：{resultModel.Level}";
            }
            else
            {
                int minutes = Mathf.FloorToInt(resultModel.SuvivedTime / 60f);
                int seconds = Mathf.FloorToInt(
                    resultModel.SuvivedTime - (minutes * 60f));
                message = $"GAME OVER...\n\n生存時間：{minutes:00}:{seconds:00}\n" +
                    $"到達レベル：{resultModel.Level}";
            }

            resultView.SetResultText(message);
        }

        private void OnDestroy() {
            if (resultView != null) {
                resultView.OnRetryAction -= RetryGame;
                resultView.OnReturnToTitleAction -= ReturnToTitle;
            }
        }

        private void RetryGame()
        {
            if (GameManager.Instance != null) {
                Destroy(GameManager.Instance.gameObject);
            }

            SceneManager.LoadScene(IN_GAME_SCENE_NAME);
        }

        private void ReturnToTitle()
        {
            SceneManager.LoadScene(TITLE_SCENE_NAME);
        }
    }
}
