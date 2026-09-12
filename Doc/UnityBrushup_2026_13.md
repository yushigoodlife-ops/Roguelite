# 最終週：ゲームに命を吹き込む！「サウンドマネージャーの実装」

## 本日の目標
いよいよ最後の講義です！これまで作ってきたゲームはとても面白いですが、何かが足りません。そう、「音」です。
今日は、ゲーム全体のBGM（音楽）とSE（効果音）、そしてそれぞれの音量を一括で管理する **「サウンドマネージャー（SoundManager）」**を作成します。

今回は総仕上げとして、**先生が用意した「ほぼ完成しているテンプレート」の空欄（課題）を、皆さん自身の力でプログラミングして完成させてください！**

---

## 1. テンプレートの準備

まずは、音を管理するためのスクリプトを用意します。
`Scripts/Core` に `Manager` フォルダを作成します。そこに `SoundManager.cs` を作成し、以下のコードをコピーして貼り付けてください。

**ファイル名： `SoundManager.cs`**
```cs
using UnityEngine;

namespace Core.Manager
{
    public class SoundManager : MonoBehaviour
    {
        // どこからでも SoundManager.Instance でアクセスできるようにする魔法（シングルトン）
        public static SoundManager Instance { get; private set; }

        [Header("スピーカーの設定")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource seSource;

        [Header("音量の設定 (0.0 ～ 1.0)")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float bgmVolume = 1f;
        [Range(0f, 1f)] public float seVolume = 1f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // シーンを切り替えても壊れないようにする
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // ==========================================
        // ▼▼▼ ここから下が今日の課題です！ ▼▼▼
        // ==========================================

        /// <summary>
        /// BGMを再生する
        /// </summary>
        public void PlayBGM(AudioClip clip)
        {
            // 課題①：ここにBGMを再生するプログラムを書こう！
        }

        /// <summary>
        /// SE（効果音）を再生する
        /// </summary>
        public void PlaySE(AudioClip clip)
        {
            // 課題②：ここにSEを再生するプログラムを書こう！
        }

        /// <summary>
        /// 音量設定が変更されたときに、実際のスピーカーの音量を更新する
        /// </summary>
        public void UpdateVolumes()
        {
            // 課題③：ここにBGMのスピーカーの音量を更新するプログラムを書こう！
        }
    }
}
```

**💡 エディタでの準備作業**
スクリプトを保存したら、Unityエディタで以下の準備をしましょう。

1. 最初のシーン（TitleScene）を開く。
2. Hierarchyに空のオブジェクトを作成し、名前を `SoundManager` にする。
3. 作成したオブジェクトに `SoundManager.cs` をアタッチする。
4. 同じオブジェクトに `Audio Source` コンポーネントを2つ 追加する。
5. 1つ目の `Audio Source` はBGM用です。`Loop` にチェックを入れ、`Play On Awake` のチェックを外します。
6. 2つ目の `Audio Source` はSE用です。`Play On Awake` のチェックを外します。
7. `SoundManager` スクリプトの `bgmSource` の枠に1つ目のAudio Sourceを、`seSource` の枠に2つ目のAudio Sourceをドラッグ＆ドロップでセットします。

## 2. 💻 本日の課題（プログラミング演習）
準備ができたら、スクリプトの空欄（課題①〜③）を埋めていきましょう！

**課題①：BGMを再生する処理を作ろう**<br>
PlayBGM メソッドの中身を完成させてください。

**課題②：SE（効果音）を再生する処理を作ろう**<br>
PlaySE メソッドの中身を完成させてください。SEは銃を撃つたびに鳴るので、音が重なっても途切れない特別なメソッドを使います。

**課題③：音量を更新する処理を作ろう**<br>
ゲーム中に設定画面などで音量を変えたとき、即座にBGMの音量が変わるように `UpdateVolumes` メソッドを完成させてください。

## 3. 実装したサウンドマネージャーを使ってみよう！
コードが完成したら、実際のゲームのスクリプトから呼び出してみましょう。<br>
例えば、銃を撃つ処理（PlayerController）に効果音を追加してみます。

ファイル名： PlayerController.cs（追加部分のみ）
``` cs
[Header("サウンド")]
    [SerializeField] private AudioClip shootSE; // インスペクターで銃の音をセットする

    // ... 

    private void Shoot()
    {
        // 銃を撃つ処理のどこかに、以下の1行を追加するだけ！
        if (shootSE != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(shootSE);
        }

        // ... 既存の処理 ...
    }
```

同じように、`GameManager` でタイトル画面が表示された時に `SoundManager.Instance.PlayBGM(titleBGM);` を呼んだりして、ゲーム全体に音を散りばめていきましょう。