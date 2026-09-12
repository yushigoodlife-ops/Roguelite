# 第7週 特別編：CSV一括変換ツールの裏側！「リフレクション」徹底解剖

この資料は、第7週で配布されたツール `MasterDataImporter.cs` の中身が「どうやって動いているのか」を知りたい、意欲的な中級者向けの特別解説書です。

ゲーム開発の現場には、プレイヤーが遊ぶゲーム本体を作るプログラマーだけでなく、**「開発チームの作業を自動化し、劇的に楽にするためのツールを作るプログラマー」** がいます（ツールエンジニアやテクニカルアーティストと呼ばれます）。

このスクリプトには、そんなプロのツール開発者が使う **「エディタ拡張」** と **「リフレクション」** という強力な魔法が詰め込まれています。順番に読み解いていきましょう！

## 1. ツールをUnityのメニューに追加する魔法

スクリプトの上の方に、見慣れない記述があります。
```cs
[MenuItem("Tools/CSVを一括でMasterDataに変換")]
public static void GenerateAllFromCSV()
{
    // ...
```
この `[MenuItem("...")]` という属性（アトリビュート）をつけるだけで、Unityの上部メニューに自作のボタンを追加できます。ボタンが押されると、その下にある `static` なメソッドが実行されます。<br>
つまり、このツールは **「ゲームを実行していなくても、Unityの編集画面上で動くプログラム（エディタ拡張）」** なのです。

## 2. 文字列から「クラスの設計図」を探し出す魔法
通常、C#では `EnemyData` などのクラスを使うとき、プログラムに直接その名前を書きます。<br>
しかし、このツールは「どんなファイル名のCSVが来るか」事前に分かりません。そこで活躍するのが **リフレクション（Reflection）** という技術です。
```cs
// ファイル名（例："EnemyData"）を取得
string fileName = Path.GetFileNameWithoutExtension(csvPath);

// ファイル名から生成すべきSOのクラス名とレコードのクラス名を推測
Type soType = Type.GetType(string.Format(MASTER_DATA_CONTAINER_FORMAT, fileName));
Type recordType = Type.GetType(string.Format(MASTER_DATA_RECORD_FORMAT, fileName));
```
`Type.GetType("クラスの名前")` は、「この名前の設計図（クラス）はありますか？」とプログラム全体から探し出す魔法です。<br>
（※ `Core.MasterData.EnemyData,Assembly-CSharp` のように、どこに置いてあるかという住所も細かく指定して確実に探し出しています）

## 3. 空のリストを「動的」に作る魔法
設計図が見つかったら、それを元に空っぽの ScriptableObject（SO）と、データをたくさん入れるための List（リスト） を作ります。
```cs
// 新しいSOインスタンスを作成
ScriptableObject soInstance = ScriptableObject.CreateInstance(soType);

// データを格納するリストのインスタンスを作成
Type listType = typeof(List<>).MakeGenericType(recordType);
IList listInstance = (IList)Activator.CreateInstance(listType);
```
ここもリフレクションのすごいところです。普段なら `new List<EnemyDataRecord>()` と書くところを、「さっき見つけた `recordType` 用のリストを作って！」と、プログラムの実行中に **動的にリストを生成（MakeGenericType）** しています。

## 4. CSVを読み込んで分割する
ここは基本のおさらいです。CSVの中身をテキストとして読み込みます。
```cs
// CSVの中身を読み込む
string[] lines = File.ReadAllLines(csvPath);

if (lines.Length >= 2)
{
    // 1行目はヘッダー（IdやAttackPowerなど）
    string[] headers = lines[0].Split(',');
    
    // ...
```
`ReadAllLines` で改行ごとに切り分け、さらに `Split(',') `でカンマごとに切り分けます。<br>
1行目（ヘッダー）は、後でデータを当てはめるための「変数名」として使います。

## 5. 禁断の魔法「リフレクション」で値を流し込む
ここがこのツールの心臓部です。CSVから取り出した文字（例：`"100"`）を、対応する変数（例：`MaxHp`）に代入します。
```cs
// ヘッダーの名前と同じプロパティを設計図から探し出す
PropertyInfo property = recordType.GetProperty(headerName, BindingFlags.Public | BindingFlags.Instance);

if (property != null && property.CanWrite)
{
    // プロパティの型（intか、stringか等）を調べる
    Type propType = property.PropertyType;
    
    // 文字列を、正しい型に変換する（自作メソッド）
    var value = ConvertPrimitiveOrEnumValue(stringValue, propType);
    
    // 強制的に値を書き込む！
    property.SetValue(recordInstance, value, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
}
```
・`GetProperty(headerName)`: 例えば `"MaxHp"` という文字列を使って、設計図から `MaxHp` プロパティを探し出します。<br>
・`SetValue(...)`: 見つけたプロパティに値をセットします。このとき、引数に `BindingFlags.NonPublic` を渡すことで、`private set` **で保護されているプロパティであっても、セキュリティを突破して強制的に書き込む** ことができます。まさにハッカーの技術です！

**※文字列を正しい型に変換する (`ConvertPrimitiveOrEnumValue`)**

CSVから読み込んだデータは、すべて「文字列（string）」です。これをそのまま `int` の変数に入れようとするとエラーになります。<br>
スクリプトの下部にある `ConvertPrimitiveOrEnumValue` メソッドでは、「このプロパティが `int` なら `int.Parse` で変換する」「`bool` なら `bool.Parse` で変換する」といった地道な変換作業を行ってくれています。

**【Enum（列挙型）の扱いについての工夫】**<br>
実はこのメソッドでは、複雑な `Enum`（列挙型）を直接文字から変換するのは非常に難しいため、対応させていません。<br>
そのため、CSVデータ側には `SemiAuto` のような文字を書くのではなく、**`0` や `1` といった Index（数値）** を指定しておきます。そしてプログラム側で実際に使うときに `(FireType)currentWeapon.FireType` のように **キャスト（型変換）** するという工夫をしています。文字のスペルミスによるエラーを防ぐための、実務的なテクニックです。

# 6. 完成したデータをUnityに保存する
最後に、値がぎっしり詰まったリストを、**ScriptableObject** の `Records` という変数にセットし、ファイルとして保存します。
```cs
// SOの "Records" というプロパティを探して、完成したリストをセット！
PropertyInfo recordProp = soType.GetProperty("Records", BindingFlags.Public | BindingFlags.Instance);
if (recordProp != null && recordProp.CanWrite)
{
    recordProp.SetValue(soInstance, listInstance, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
}

// メモリ上のデータを、実際のファイル（.asset）として保存する
AssetDatabase.CreateAsset(soInstance, exportPath);

// 保存を確実にして、Unityのプロジェクトウィンドウを更新する
AssetDatabase.SaveAssets();
AssetDatabase.Refresh();
```
`AssetDatabase` は、Unityのエディタ上でのみ使える特別な機能です。これを使うことで、プログラムから直接ファイルを作成したり、フォルダを作ったりすることができます。

## 7. Addressablesへの自動登録
さらにこのツールでは、保存したアセットを自動的に **Addressables** に登録する処理も組み込むことができます。
```cs
// 保存したアセットのGUID（固有ID）を取得
string guid = AssetDatabase.AssetPathToGUID(exportPath);

// 現在のAddressablesの設定ファイルを取得する
AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

if (settings != null)
{
    // デフォルトのグループにアセットを自動登録する
    AddressableAssetGroup group = settings.DefaultGroup;
    AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

    if (entry != null)
    {
        // アドレスをCSVの名前（例: "EnemyData"）に自動設定！
        entry.address = fileName;

        // ラベルもファイル名と同じものを自動で付与する！
        settings.AddLabel(fileName);
        entry.SetLabel(fileName, true, true);
    }
}
```
わざわざ `Addressables Groups` のウィンドウを開いて、アセットをドラッグ＆ドロップし、ラベルを手作業で打ち込む……という面倒な作業も、エディタ拡張の力を使えばこのように完全に全自動化できるのです！

## まとめ
このツールを使えば、これからどんなに新しいキャラクターや武器の種類が増えても、新しくプログラミングをする必要はありません。CSVを用意してメニューを押すだけで、リフレクションが自動的に設計図を解析し、データを流し込み、Addressablesの登録まで終わらせてくれます。

「リフレクション」や「エディタ拡張」は、C#やUnityの中でもかなり高度な技術ですが、これを使いこなせるようになれば、あなたのプログラマーとしての市場価値は跳ね上がります。ぜひ、このコードを何度も読んで、魔法の仕組みを自分のものにしてください！