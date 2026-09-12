>以下の各問題のC#コードは、コンパイル時に記載されたビルドエラーが発生します。<br>
>エラー文を手がかりに、原因となっている誤った記述を線（取り消し線）で消し、その横や空いているスペースに正しいコードを記述して修正しなさい。

### 第1問：定義されていない変数の使用
**【問題文】**<br>
以下の `PlayerScore` クラスをコンパイルしたところ、以下のビルドエラーが発生しました。誤っている箇所を線で消し、正しいコードに修正しなさい。<br>
**エラー文:**<br>
`エラー CS0103: 現在のコンテキストに 'curretScore' という名前は存在しません。`

**【問題コード】**
``` cs
public class PlayerScore
{
    private const int INITIAL_SCORE = 100;
    private int currentScore;

    public int CurrentScore { get; private set; }

    public PlayerScore()
    {
        currentScore = INITIAL_SCORE;
        CurrentScore = INITIAL_SCORE;
    }

    public void AddScore(int scoreValue)
    {
        curretScore += scoreValue; // エラー箇所
    }
}
```

### 第2問：定義されていない変数の使用
**【問題文】**<br>
以下の `GameController` クラス内のコードで、以下のビルドエラーが発生しました。誤っている箇所を線で消し、正しいコードに修正しなさい。<br>
**エラー文:**<br>
`エラー CS1061: 'Enemy' に 'Health' の定義が含まれておらず、型 'Enemy' の最初の引数を受け付けるアクセス可能な拡張メソッド 'Health' が見つかりませんでした。`

**【問題コード】**
``` cs
public class Enemy
{
    private const int MAX_HP = 50;
    
    public int HitPoint { get; private set; }

    public Enemy()
    {
        HitPoint = MAX_HP;
    }
}

public class GameController
{
    private const int DAMAGE = 10;

    public void AttackEnemy(Enemy targetEnemy)
    {
        int currentHp = targetEnemy.Health - DAMAGE; // エラー箇所
    }
}
```

### 第3問：｛｝の置き間違いによるエラー
**【問題文】**<br>
以下の `NumberChecker` クラスをコンパイルしたところ、以下のビルドエラーが発生しました。括弧の配置を直し、正しいコードに修正しなさい。（誤った記述を線で消し、正しい位置に追記すること）<br>
**エラー文:**<br>
`エラー CS0161: 'NumberChecker.IsPositive(int)': コード パスの実行中に値を返さない可能性があります。`

**【問題コード】**
``` cs
public class NumberChecker
{
    private const int THRESHOLD = 0;

    public bool IsPositive(int checkNumber)
    {
        if (checkNumber > THRESHOLD)
        {
            return true;
        return false;
        }
    }
}
```

### 第4問：定義していない関数の使用
**【問題文】**<br>
以下の `DataProcessor` クラスで、以下のビルドエラーが発生しました。誤っている箇所を線で消し、正しいコードに修正しなさい。<br>
**エラー文:**<br>
`エラー CS0103: 現在のコンテキストに 'InitalizeData' という名前は存在しません。`

**【問題コード】**
``` cs
public class DataProcessor
{
    public void InitializeData()
    {
        // 初期化処理
    }

    public void StartProcess()
    {
        InitalizeData(); // エラー箇所
    }
}
```

### 第5問：静的（static）メソッドから非静的（インスタンス）メンバへのアクセス
**【問題文】**<br>
以下の `ScoreManager` クラスをコンパイルしたところ、以下のビルドエラーが発生しました。誤っている箇所を線で消し、正しいコードに修正しなさい。（ヒント：このメソッドはインスタンスごとのスコアをリセットする目的で作られています）<br>
**エラー文:**<br>
`エラー CS0120: 静的でないフィールド、メソッド、またはプロパティ 'ScoreManager.CurrentScore' で、オブジェクト参照が必要です。`

**【問題コード】**
``` cs
public class ScoreManager
{
    private const int INITIAL_SCORE = 0;
    private int currentScoreValue;

    public int CurrentScore { get; private set; }

    public ScoreManager()
    {
        CurrentScore = INITIAL_SCORE;
        currentScoreValue = INITIAL_SCORE;
    }

    public static void ResetScore() // エラー箇所
    {
        CurrentScore = INITIAL_SCORE;
    }
}
```

### 第6問：型の不一致
**【問題文】**<br>
以下の `PlayerStats` クラス内の `ApplyDamage` メソッドで、以下のビルドエラーが発生しました。誤っている箇所を線で消し、正しいコードに修正しなさい。<br>
**エラー文:**<br>
`エラー CS0019: 演算子 '-' を 'int' と 'string' 型のオペランドに適用することはできません。`

**【問題コード】**
``` cs
public class PlayerStats
{
    private const int DEFAULT_HEALTH = 100;

    public int HealthPoint { get; private set; }

    public PlayerStats()
    {
        HealthPoint = DEFAULT_HEALTH;
    }

    public void ApplyDamage(string damageValue)
    {
        HealthPoint = HealthPoint - damageValue;
    }
}
```

### 第7問：すべてのコードパスで値が返されていない
**【問題文】**<br>
以下の `MathUtility` クラスをコンパイルしたところ、以下のビルドエラーが発生しました。このメソッドは、条件を満たさない場合は `false` を返す必要があります。不足しているコードを正しい位置に追記して修正しなさい。<br>
**エラー文:**<br>
`エラー CS0161: 'MathUtility.IsOverThreshold(int)': コード パスの実行中に値を返さない可能性があります。`

**【問題コード】**
``` cs
public class MathUtility
{
    private const int THRESHOLD_VALUE = 10;

    public bool IsOverThreshold(int checkValue)
    {
        if (checkValue > THRESHOLD_VALUE)
        {
            return true;
        }

    }
}
```

### 第8問：キャスト不足による型の不一致
**【問題文】**<br>
以下の `DamageCalculator` クラスをコンパイルしたところ、以下のビルドエラーが発生しました。誤っている箇所を線で消し、正しいコードに修正しなさい。<br>
**エラー文:**<br>
`エラー CS0266: 型 'float' を 'int' に暗黙的に変換できません。明示的な変換が存在します (cast が不足していないかどうかを確認してください)`

**【問題コード】**
``` cs
public class DamageCalculator
{
    private const float CRITICAL_MULTIPLIER = 1.5f;

    public int CalculateCriticalDamage(int baseDamage)
    {
        int finalDamage = baseDamage * CRITICAL_MULTIPLIER;
        return finalDamage;
    }
}
```

### 第9問：組み込み型のスペルミス
**【問題文】**<br>
以下の `UserProfile` クラスで、以下のビルドエラーが発生しました。誤っている箇所を線で消し、正しいコードに修正しなさい。<br>
**エラー文:**<br>
`エラー CS0246: 型または名前空間の名前 'sting' が見つかりませんでした (using ディレクティブまたはアセンブリ参照が指定されていることを確認してください)`

**【問題コード】**
``` cs
public class UserProfile
{
    private const string DEFAULT_NAME = "Guest";

    public sting UserName { get; private set; }

    public UserProfile()
    {
        UserName = DEFAULT_NAME;
    }
}
```

### 第10問：｛｝の位置の置き間違い
**【問題文】**<br>
以下の `ActionManager` クラスをコンパイルしたところ、以下のビルドエラーが発生しました。クラスの範囲（スコープ）がおかしくなっています。誤って配置されている括弧を線で消し、正しい位置に書き直しなさい。<br>
**エラー文:**<br>
`エラー CS0116: 名前空間にフィールドやメソッドなどのメンバーを直接含めることはできません`

**【問題コード】**
``` cs
public class ActionManager
{
    public void Jump()
    {
        // ジャンプ処理
    }
}

    public void Attack()
    {
        // 攻撃処理
    }


```