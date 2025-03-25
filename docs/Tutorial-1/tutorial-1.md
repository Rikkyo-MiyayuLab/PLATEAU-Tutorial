# PLATEAU チュートリアル <br> 避難所配置の最適化シミュレーション 実装ガイド

# 目次
- [PLATEAU チュートリアル  避難所配置の最適化シミュレーション 実装ガイド](#plateau-チュートリアル--避難所配置の最適化シミュレーション-実装ガイド)
- [目次](#目次)
- [目的](#目的)
- [作成環境](#作成環境)
- [事前準備](#事前準備)
  - [Unityのインストール](#unityのインストール)
  - [ML-Agentsのプロジェクトへの導入](#ml-agentsのプロジェクトへの導入)
  - [NewtonSoft.jsonのプロジェクトへの導入](#newtonsoftjsonのプロジェクトへの導入)
- [実装手順](#実装手順)
  - [1.モデル都市のインポート](#1モデル都市のインポート)
  - [2. 必要素材の準備](#2-必要素材の準備)
    - [2-1. 避難者のプレハブの作成](#2-1-避難者のプレハブの作成)
    - [2-2. マテリアルの作成](#2-2-マテリアルの作成)
  - [3. 避難所の候補地の事前設定](#3-避難所の候補地の事前設定)
  - [4. 避難者が移動する経路を作る（tranへのナビゲーションメッシュの適用）](#4-避難者が移動する経路を作るtranへのナビゲーションメッシュの適用)
  - [5. 避難者の生成ポイントを作成する](#5-避難者の生成ポイントを作成する)
  - [6. プログラムファイルの作成](#6-プログラムファイルの作成)
  - [7. シミュレーション環境パラメータの設定](#7-シミュレーション環境パラメータの設定)
    - [6-1. `ShelterManagementAgent`の設定](#6-1-sheltermanagementagentの設定)
    - [6-2. `ShelterEnvManager`の設定](#6-2-shelterenvmanagerの設定)
      - [6-2-1. 避難者生成モードについて](#6-2-1-避難者生成モードについて)
    - [6-3. `Evacuee`の設定](#6-3-evacueeの設定)
    - [6-4. ハイパーパラメータの設定](#6-4-ハイパーパラメータの設定)
      - [`trainer_type : ppoまたはsac`](#trainer_type--ppoまたはsac)
      - [`buffer_size : 一般的な値：2048 - 409600 (ppoの場合)`](#buffer_size--一般的な値2048---409600-ppoの場合)
      - [`batch_size：一般的な値：32 - 512（離散行動でppoの場合）`](#batch_size一般的な値32---512離散行動でppoの場合)
      - [`beta：一般的な値：0.0001～0.01`](#beta一般的な値00001001)
      - [`network_settings > normalize`](#network_settings--normalize)
      - [`network_settings > hidden_units` : 一般的な値 : 32～512](#network_settings--hidden_units--一般的な値--32512)
      - [`network_settings >num_layers` : 一般的な値 : 1～4](#network_settings-num_layers--一般的な値--14)
  - [8. 学習の実行と結果の確認](#8-学習の実行と結果の確認)
    - [8-1. 学習の実行](#8-1-学習の実行)
    - [8-2. 学習結果の分析](#8-2-学習結果の分析)
    - [8-3. 学習済みモデルを使用してシミュレーションを動かす](#8-3-学習済みモデルを使用してシミュレーションを動かす)


# 目的
津波や洪水等の災害発生時において、最適な避難所の配置計画を作成することを目指します。避難時間や避難率の最適化を目標に、AIに都市モデル内でどの建物を避難所にすべきかを学習させ、動的に変化する避難者の配置分布から最適な避難所配置を導きます。

# 作成環境
- Unity 2023.2.19f1
- ML-Agents toolkit Release 22
- PLATEAU SDK for Unity 2.3.2
- Windows 11

# 事前準備
## Unityのインストール
Unityアカウントを作成し、Unity Hubをインストールします。
[Unity Hub](https://unity.com/ja/download)のダウンロードページを開き、お使いのOSに合わせたバージョンをインストールしてください。

Unity Hubインストール後、Unity Hubを起動し、`インストール`タブから`Unity 2023.2.19f1`をインストールします。
![alt text](../Common/EditorInstall.png)

インストールが完了したら、`プロジェクト`タブから`新規`を選択し、`Unity 2023.2.19f1`を選択してプロジェクトを作成します。

## ML-Agentsのプロジェクトへの導入
Unityのプロジェクトを作成したら、ML-Agentsをインストールします。
インストールガイドは[こちら](https://github.com/Unity-Technologies/ml-agents/blob/release_22/docs/Installation.md)

<details>
<summary>Unityパッケージの導入</summary>

1. [リリースページ](https://github.com/Unity-Technologies/ml-agents/releases/tag/release_22)にアクセスし、`Source code(zip)`をダウンロードし、展開してください。

2. 作成したUnityプロジェクトを開き、メニューバーの`Window > PackageManager`を開き、上部 ＋ボタンから`Add packages from disk`を選択します。

    ![alt text](image-19.png)

3. エクスプローラーが開くので、展開後フォルダの以下のファイルをそれぞれ選択してください。
    ```
    com.unity.ml-agents/package.json"
    com.unity.ml-agents.extensions/package.json
    ```
</details>

<details>
<summary>Python環境の構築</summary>

ML-Agentsでモデル訓練を実行するには、Pythonパッケージの導入が必要です。
Python環境並びにパッケージのインストール方法については、お使いの環境に合わせて適宜読み替えてください。ここでは、標準のパッケージインストールツール`pip`を使用した場合のインストール方法について解説します。

1. `mlagents`のインストール
```shell
pip install mlagents==1.1.0
```
<details>
<summary>GPUを使用した学習/推論を行いたい場合</summary>

NVIDIAのGPUをお持ちの方は、モデル訓練時にGPUを使用することが可能です。これは多くの場合、CPUよりも学習時間が早くなります。

GPU を使用した学習にはCUDAと対応するPytorchのインストールが必要です。

1. CUDA版PyTorchのインストール
    ```shell
    pip install torch==2.1.1 torchvision==0.16.1 torchaudio==2.1.1 --index-url https://download.pytorch.org/whl/cu118
    ```
2. CUDA Ver 11.8のインストール
    下記URLにアクセスし、インストーラー経由でインストールします。
    ```
    https://developer.nvidia.com/cuda-11-8-0-download-archive
    ```
</details>

</details>

## NewtonSoft.jsonのプロジェクトへの導入
PLATEAUの属性情報をUnityスクリプトで利用可能にするため、JSONシリアライザー/デシリアライザーツールである Newtonsoft.json の導入を行います。

メニューバーの`Window > PackageManager`を開き、上部 ＋ボタンから`Add packages from git URL`を選択し下記のURLを入力してインストールします。
```
https://github.com/jilleJr/Newtonsoft.Json-for-Unity.git
```
![alt text](image-7.png)



# 実装手順

## 1.モデル都市のインポート
PLATEAU SDKより、使用したい都市モデルをインポートします。

※今回は題材として、横須賀市市役所周辺のモデルをインポートします。

![alt text](../Common/image-7.png)

インポート後、全体制御用のGameObjectを作成します。名前は任意ですが、今回は`Field`としておきます。

![alt text](image-13.png)

作成した`Field`オブジェクトの配下に、同様の手順でAI制御用のオブジェクトを作成します。
名前は任意ですが、今回は`ShelterSelectAgent`としておきます。

![alt text](image-26.png)

## 2. 必要素材の準備
### 2-1. 避難者のプレハブの作成
1. オブジェクトのインポート

    今回はAssetStore上にある、無料のキャラクターオブジェクトを利用します。
    - [Easy Primitive People](https://assetstore.unity.com/packages/3d/characters/easy-primitive-people-161846)

    AssetStoreからインポートし、プロジェクトに適用します。
    どのキャラクターでも構いませんが、今回使用するキャラクターは分かりやすいようにAssetフォルダ直下に配置し、名前を`Evacuee`としておきます。

2. 必要コンポーネントの追加
    `Evacuee`オブジェクトを選択し、以下のコンポーネントを追加します。
    - `Nav Mesh Agent` : キャラクターの移動を制御するコンポーネント
    - `Rigidbody` : 物理演算を適用を提供するコンポーネント
    - `Capsule Collider` : キャラクターの当たり判定を提供するコンポーネント

    ![alt text](image-6.png)

### 2-2. マテリアルの作成
AIが避難所に指定した建物とそうでない建物を視覚的に区別するために、2色のマテリアルを作成しておきます。

色は任意ですが、今回は以下のような構成で作成します。
| 色       | 意味       |
|-----------|-----------|
| 緑  | 避難所に選定された建物   | 
| 黒   | 避難所に選定されなかった建物   |

![alt text](image-8.png)


## 3. 避難所の候補地の事前設定
AIがシミュレーション中に避難所として指定できる建物の候補地を事前に設定します。
下記手順を候補地の分だけ繰り返して設定していきます。

1. 避難所かそうでないかを判別するためのタグを追加します。

    任意のオブジェクトを選択し、Inspectorの上部にある`Tag > Add Tag`を選択します。
    `+`ボタンを押下し、新しいタグ名`Shelter`を追加します。

    ![alt text](image.png)

2. 候補地とする建物（`bldg_<オブジェクトID>`）を選択し、Inspectorから先ほど追加したタグ名`Shelter`を設定します。
    ![alt text](image-14.png)

3. 必要な以下のコンポーネントを建物に追加,設定します。
    建物オブジェクトを選択した状態で、Inspector下部にある`Add Component`を押下し、以下のコンポーネントを追加してください。
    - `NavMeshObstacle` : 後述する`ナビゲーションメッシュの適用`際に必要です。追加した後は、チェックマークを外し、ディアクティベーションしてください。

    次に、避難者が建物に到着したことを検知するために、当たり判定を設定します。`MeshCollider`にある,以下の項目にチェックを入れてください。
    - `Convex`
      - `Is Trigger`
    
    ![alt text](image-5.png)

4. 建物の入り口と道路を繋ぐためにPlaneオブジェクトを追加します。
    このオブジェクトの名前を`SubTran`とし、建物と道路を繋ぐように配置してください。
    ![alt text](image-1.png)

5. 建物のゲーム内での座標位置情報を扱うため、建物の子要素に目印となるcubeオブジェクトを追加します。

    このとき、cubeオブジェクトは、`手順4`で追加したPlaneオブジェクト上で、建物の内側の位置に配置してください。

    > デフォルトでは建物（`bldg`）の座標情報は*(X,Y,Z) = (0,0,0)*で固定されています。AIへ渡す観測情報として、建物の座標情報を取得するために、代替としてcubeオブジェクトを追加しています。
    
    ![alt text](image-3.png)



## 4. 避難者が移動する経路を作る（tranへのナビゲーションメッシュの適用）

1. ナビゲーションメッシュを適用する前に、除外範囲を設定します。
    このままでは、フィールドの地形や他の建物上にもナビゲーションメッシュが適用されてしまい、避難者が通行可能になっていまうため、事前にこれらの除外設定を行います。
    <details>
    <summary>除外設定をせずにナビゲーションメッシュを適用した例</summary>

    - 青色部分がナビゲーションメッシュ適用部分で、避難者が通行できる箇所となります。
    - ![alt text](image-25.png)
    </details>

    - 地形オブジェクト`dem_<オブジェクトID>`を除外設定に追加する。
  
        Hierarchyの検索ボックスに`dem`と入力し、地形オブジェクトを選択します。`Add Component`から`NavMesh Obstacle`コンポーネントを追加し、チェックマークを外してディアクティベーションします。
        ![alt text](image-10.png)

    - 建物オブジェクト`bldg_<オブジェクトID>`を除外設定に追加する。
  
        Hierarchyの検索ボックスに`bldg`と入力し、建物オブジェクトを選択します。`Add Component`から`NavMesh Obstacle`コンポーネントを追加し、チェックマークを外してディアクティベーションします。
        ![alt text](image-11.png)

2. 避難者が移動する経路を作成するために、都市モデル内の道路データに、ナビゲーションメッシュを適用します。
    > ナビゲーションメッシュとは
    > NPCの移動可能領域を設定するUnityの機能です。ナビゲーションメッシュを適用することで、NPCが移動可能な範囲を設定し、移動経路を計算することができます。

    Hirarchyビューから、都市モデル内の道路データ`tran`と建物の入り口と道路を繋ぐ`SubTran`を検索し、`Add Component`から`NavMesh Surface`を選択します。

    ![alt text](image-9.png)

    > このとき、`tran_<オブジェクトID>`と書かれているオブジェクトを選択すると、オブジェクト１つ１つにナビゲーションメッシュを適用することができますが、個数が膨大なため非常に時間がかかります。道路全体に適用する場合は、親オブジェクトである`tran`を選択し、ナビゲーションメッシュを適用してください。

    `NavMesh Surface`コンポーネントを追加したら、`Bake`ボタンを押下し、ナビゲーションメッシュを生成・適用します。

    ![alt text](image-12.png)

## 5. 避難者の生成ポイントを作成する
[2-1. 避難者のプレハブの作成](#2-1-避難者のプレハブの作成)で作成した`Evacuee`オブジェクトを、環境内に生成する為の生成ポイントを作成します。
上部メニューバーから「Window → 3D Object → Capsule」を選択し、生成したCapsuleオブジェクトを`SpawnPoint`という名前に設定します。

![alt text](image-20.png)

作成したら、識別用のタグを付与します。`Inspectorビュー`の上部にある`tag`プルダウンを開き、`Add Tag...`から`SpawnPos`という名前のタグを追加します。追加したら、`SpawnPoint`オブジェクトを選択し、`SpawnPos`タグを選択します。

![alt text](image-22.png)

次に、`Inspectorビュー`の下部にある、`Add Component`から`New Script`を選択し、`EvacueeSpawnPoint`という名前でスクリプトを作成します。

![alt text](image-21.png)



## 6. プログラムファイルの作成
ここからは、C# 用いて、AIやシミュレーションの挙動を制御するスクリプトファイルを作成します。

以下の各プログラムを作成し、各オブジェクトへアタッチしていきます。

<details>
<summary>避難者用プログラムの作成</summary>

- `Evacuee.cs` 
    ```cs
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AI;

    /// <summary>
    /// 避難者の制御を行うクラス
    /// </summary>
    public class Evacuee : MonoBehaviour {
        
        [Header("Movement Target")]
        public GameObject Target; // 現在の移動目標
        private NavMeshAgent NavAgent; // NavMeshAgentコンポーネント
        private EnvManager _env; // ShelterEnvManagerの参照
        private bool isEvacuating = false; // 避難処理中のフラグ。当たり判定により発火するため、複数回避難処理が行われるのを防ぐためのフラグ
        private List<string> excludeShelters; //1度避難したタワーのUUIDを格納するリスト
        void Awake() {
            NavAgent = GetComponent<NavMeshAgent>();    
            excludeShelters = new List<string>(); 

            _env = GetComponentInParent<EnvManager>();
            _env.Agent.OnDidActioned += () => {
                // エージェントが建物を選択したことを検知して最短距離の避難所を探す
                if(this != null && this.gameObject.activeSelf) {
                    List<GameObject> towers = SearchShelters();
                    if(towers.Count > 0) {
                        Target = towers[0]; //最短距離のタワーを目標に設定
                        NavAgent.SetDestination(Target.transform.position);
                    }
                }
            };
        }


        
        /// <summary>
        /// タグ名から避難所を検索する。フィールドに存在する全てのタワーを検索し、距離別にソートして返す
        /// </summary>
        /// <param name="excludeTowerUUIDs">除外するタワーのUUID.未指定の場合はnull</param>
        /// <returns>localField内のTowerオブジェクトのリスト</returns>
        private List<GameObject> SearchShelters(List<string> excludeTowerUUIDs = null) {
            // タグ名から避難所を検索する
            GameObject[] towers = GameObject.FindGameObjectsWithTag("Shelter");
            GameObject[] constShelters = GameObject.FindGameObjectsWithTag("ConstShelter");
            List<GameObject> Iterates = new List<GameObject>();
            foreach (var shelter in towers) {
                Iterates.Add(shelter);
            }
            foreach (var shelter in constShelters) {
                Iterates.Add(shelter);
            }
            // 訪れたことのない避難所を探す
            List<GameObject> sortedTowerPoints = new List<GameObject>();
            foreach (var tower in Iterates) {
                if(excludeTowerUUIDs != null && excludeTowerUUIDs.Contains(tower.GetComponent<Shelter>().uuid)) {
                    continue;
                }
                GameObject point = tower.transform.GetChild(0).gameObject; // 避難所に設置した目印オブジェクトを取得
                sortedTowerPoints.Add(point);
            }
            // NOTE: エピソード更新時にgameObjectがnullになることがあるので、nullチェックを行う
            if(this != null) {
                // 距離別にソート
                sortedTowerPoints.Sort((a, b) => Vector3.Distance(a.transform.position, transform.position).CompareTo(Vector3.Distance(b.transform.position, transform.position))); 
            }
            return sortedTowerPoints;
        }

        /// <summary>
        /// 避難を行う処理
        /// 避難所のオブジェクトにアタッチされ、当たり判定により呼び出される 
        /// </summary>
        public void Evacuation(Shelter shelter) {
            if(isEvacuating) {
                return;
            }
            isEvacuating = true;
            // キャパシティーがある場合、避難処理を行う
            if(shelter.currentCapacity > 0) {
                shelter.NowAccCount++;
                gameObject.SetActive(false);
            } else { //キャパシティがいっぱいの場合、次の避難所を探す
                excludeShelters.Add(shelter.uuid);
                List<GameObject> shelters = SearchShelters(excludeShelters);
                if(shelters.Count > 0) {
                    Target = shelters[0]; //最短距離のタワーを目標に設定
                    NavAgent.SetDestination(Target.transform.position);
                }
            }
            isEvacuating = false;
        }

    }

    ```
    作成後、Assetsフォルダ内にある`Evacuee`オブジェクトにアタッチしてください。
</details>

<details>
<summary>避難者生成ポイントのプログラムの作成</summary>

- `EvacueeSpawnPoint.cs`
  
```cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// カスタムスポーン用のポイント
/// </summary>
public class EvacueeSpawnPoint : MonoBehaviour {
    
    public GameObject EvacueePrefab; // 避難者のプレハブ
    public float SpawnRadius = 10f; // 生成半径（この半径内にある道路上に生成）
    public int SpawnSize = 50; // 生成する人数
    private GameObject rangeIndicator; // スポーン範囲の表示オブジェクト

    void Start() {
        ShowRangeOff(); // 初期状態では非表示
    }

    public void SpawnEvacuee() {
        Vector3 spawnPos = transform.position + Random.insideUnitSphere * SpawnRadius;
        spawnPos.y = transform.position.y; // 地面に沿わせる
        GameObject evacuee = Instantiate(EvacueePrefab, spawnPos, Quaternion.identity);
        evacuee.transform.parent = transform.parent;
        evacuee.tag = "Evacuee";
    }

    /// <summary>
    /// ランタイムでスポーン範囲を半透明で表示（ミニマップ用レイヤー設定）
    /// </summary>
    public void ShowRangeOn() {
        if (rangeIndicator == null) {
            rangeIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rangeIndicator.transform.SetParent(transform);
            rangeIndicator.transform.localPosition = Vector3.zero;
            rangeIndicator.transform.localScale = new Vector3(SpawnRadius * 2, SpawnRadius * 2, SpawnRadius * 2);


            // マテリアルの設定（半透明）
            Material transparentMaterial = new Material(Shader.Find("Standard"));
            transparentMaterial.color = new Color(200f, 0f, 0f, 0.7f); // 半透明の緑色
            transparentMaterial.SetFloat("_Mode", 3); // 透過設定
            transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transparentMaterial.SetInt("_ZWrite", 0);
            transparentMaterial.DisableKeyword("_ALPHATEST_ON");
            transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
            transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            transparentMaterial.renderQueue = 3000;

            rangeIndicator.GetComponent<Renderer>().material = transparentMaterial;
            rangeIndicator.GetComponent<Collider>().enabled = false; // 当たり判定を無効化
        }
    }

    /// <summary>
    /// スポーン範囲を非表示
    /// </summary>  
    public void ShowRangeOff() {
        if (rangeIndicator != null) {
            Destroy(rangeIndicator);
            rangeIndicator = null;
        }
    }
}
```

作成後、`SpawnPoint`オブジェクトにアタッチしてください。
</details>


<details>
<summary> 避難所建物用プログラムの作成 </summary>

- `Shelter.cs`
  ```cs
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 避難所に関するスクリプト（オブジェクト１台分）
    /// 現在の収容人数や、受け入れ可否等のデータを用意
    /// </summary>
    public class Shelter : MonoBehaviour{
        public int MaxCapacity = 10; //最大収容人数
        public int NowAccCount = 0; //現在の収容人数
        public int currentCapacity; //現在の受け入れ可能人数：最大収容人数 - 現在の収容人数

        public string uuid; //タワーの識別子
        /**Events */
        public delegate void AcceptRejected(int NowAccCount) ; //収容定員が超過した時に発火する
        public AcceptRejected onRejected;

        private EnvManager _env;

        void Start() {
            _env = GetComponentInParent<EnvManager>();
            _env.OnEndEpisode += (float _) => {
                // 環境側のエピソード終了時に収容人数をリセット
                NowAccCount = 0;
            };
        }


        /// <summary>
        /// リアルタイムで収容人数を更新
        /// </summary>
        void Update() {
            currentCapacity = MaxCapacity - NowAccCount;
            if (currentCapacity <= 0) {
                onRejected?.Invoke(NowAccCount);
            }
        }

        /// <summary>
        /// 避難者オブジェクトが建物に到達したときに呼び出される。当たり関数
        /// </summary>
        /// <param name="other"></param>
        void OnTriggerEnter(Collider other) {
            bool isEvacuee = other.CompareTag("Evacuee");
            if (isEvacuee) {
                Evacuee evacuee = other.GetComponent<Evacuee>();
                evacuee.Evacuation(this);
            }
            
            
        }
    }

  ```
  このコンポーネントは、実行時に自動的に各避難所候補地へアタッチされます。
</details>

<details>
<summary>AI用プログラムの作成</summary>

- `ShelterAgent.cs`
  ```cs
    using System.Collections;
    using System.Collections.Generic;
    using System;
    using System.Linq;
    using UnityEngine;
    using Unity.MLAgents;
    using Unity.MLAgents.Actuators;
    using Unity.MLAgents.Sensors;

    /// <summary>
    /// 実装する全てのエージェントは、
    /// ML-AgentsパッケージにあるAgentクラスを継承して実装します。
    /// https://docs.unity3d.com/Packages/com.unity.ml-agents@3.0/api/Unity.MLAgents.Agent.html
    /// </summary>

    public class ShelterManagementAgent : Agent {
        
        public GameObject[] ShelterCandidates; //エージェントが操作する避難所の候補リスト
        public Material SelectedMaterial; // 避難所としてその建物を選択している時のマテリアル
        public Material NonSelectMaterial; // 避難所としてその建物を選択していない時のマテリアル
        public Action OnDidActioned; // 行動選択後に呼ばれるイベント関数(避難者の初期化にて使用します)

        // episode, step, 各避難所候補の選択状況のリスト(true or false)
        public List<Tuple<int, int, List<bool>>> ActionLogs = new List<Tuple<int, int, List<bool>>>(); 
        public bool Disabled = false;
        public List<GameObject> ConstBldgs;
        private EnvManager _env; // 環境管理クラスの参照
        
        void Start() {
            _env = GetComponentInParent<EnvManager>();
            //Academy.Instance.AutomaticSteppingEnabled = true;

        }

        public override void Initialize() {
            Time.timeScale = 100f;
            if(ShelterCandidates.Length == 0) {
                //Debug.LogError("No shelter candidates");
                // NOTE: 予め候補地は事前に設定させておくこと
                ShelterCandidates = GameObject.FindGameObjectsWithTag("Shelter");
            }
        }

        /// <summary>
        /// Agent.EndEpisode()後に呼ばれる
        /// 環境の初期化処理実行後に、エージェントの行動をリクエストします。 
        /// </summary>
        public override void OnEpisodeBegin() {
            _env.OnEpisodeBegin();
            RequestDecision(); // 行動選択をリクエスト
        }

        public void OnEndEpisode() {
            // データの保存とActionLogsの初期化
            // 避難所の建物IDを取得
            string[] shelterIds = new string[ShelterCandidates.Length];
            for(int i = 0; i < ShelterCandidates.Length; i++) {
                shelterIds[i] = ShelterCandidates[i].name;
            }
            // CSVデータの作成
            string[] headers = new string[ShelterCandidates.Length + 2];
            headers[0] = "Episode";
            headers[1] = "Step";
            Array.Copy(shelterIds, 0, headers, 2, shelterIds.Length);
            Utils.SaveResultCSV(
                headers,
                ActionLogs,
                (data) => new string[] { data.Item1.ToString(), data.Item2.ToString() }.Concat(data.Item3.ConvertAll(x => x ? "1" : "0")).ToArray(),
                $"{_env.recordID}/ActionLog_Episode_{_env.currentEpisodeId}.csv"
            );

            ActionLogs.Clear(); // 行動ログの初期化
        }

        /// <summary>
        /// ここでは環境内の状態をエージェントに観測させるための処理を記述します。
        /// モデルへの入力として使用されます。
        /// 今回は観測情報として以下の情報をモデルに入力します。
        /// 1. 避難所候補地の位置情報（x, y, z）
        /// 2. 避難所候補地の収容可能人数
        /// 3. 環境内の避難者数
        /// 4. 避難者の位置情報（x, y, z）
        /// CollectObservations内で観測できるデータの種類については下記docsを参照してください。
        /// https://docs.unity3d.com/Packages/com.unity.ml-agents@3.0/api/Unity.MLAgents.Agent.html#Unity_MLAgents_Agent_CollectObservations_Unity_MLAgents_Sensors_VectorSensor_
        /// </summary>
        /// <param name="sensor">この仮引数にあるAddObservationメソッドを通じて観測を行います</param>
        public override void CollectObservations(VectorSensor sensor) {

            // 避難所候補地のリストを巡回し、各避難所候補地の位置情報と収容可能人数を入力
            foreach(GameObject shelter in ShelterCandidates) {
                sensor.AddObservation(shelter.transform.GetChild(0).gameObject.transform.position);
                sensor.AddObservation(shelter.GetComponent<Shelter>().currentCapacity);
            }

            // NOTE : 観測のタイミングで避難者が避難してGameObjectが消えることがあるので、ここでコピーを作成
            List<GameObject> evacuees = new List<GameObject>(_env.Evacuees);
            sensor.AddObservation(evacuees.Count); // 環境内の避難者数

            // 避難者のリストを巡回し、各避難者の位置情報を追加
            foreach(GameObject evacuee in evacuees) {
                if(evacuee != null) {
                    sensor.AddObservation(evacuee.transform.position);
                } else {
                    // NOTE : 観測サイズは固定でなければならないため、万一取得に失敗した場合は0ベクトルを追加
                    sensor.AddObservation(Vector3.zero);
                }
            }
            

        }

        /// <summary>
        /// エージェントの行動を実装するメソッドです。
        /// 今回は避難所候補地のリストの中から、それぞれの建物を避難所とするか否かを選択します。
        /// 選択する場合は1、選択しない場合は0を選択します。
        /// https://docs.unity3d.com/Packages/com.unity.ml-agents@3.0/api/Unity.MLAgents.Agent.html#Unity_MLAgents_Agent_OnActionReceived_Unity_MLAgents_Actuators_ActionBuffers_
        /// </summary>
        /// <param name="actions">モデルの行動出力を受け取るための仮引数で、この値を元に環境に行動を反映させます</param>
        public override void OnActionReceived(ActionBuffers actions) {
            var Selects = actions.DiscreteActions; //エージェントの選択。環境の候補地配列と同じ順序。[<建物１の避難所選択結果 0 or 1>, <建物２の避難所選択結果 0 or 1>, ...]

            List<bool> selectList = new List<bool>();
            if(Selects.Length != ShelterCandidates.Length) {
                Debug.LogError("Invalid action size : 避難所候補地のサイズとエージェントの選択サイズが不一致です");
                return;
            }

            // モデルの行動出力リストを巡回し、0の場合は避難所として選択しない、1の場合は選択する
            for(int i = 0; i < Selects.Length; i++) {
                int select = Selects[i]; // 0:非選択、1:選択
                GameObject Shelter = ShelterCandidates[i];
                if(select == 1) {
                    _env.CurrentShelters.Add(Shelter);
                    Shelter.tag = "Shelter";
                    Shelter.GetComponent<MeshRenderer>().material = SelectedMaterial;
                    selectList.Add(true);
                } else if(select == 0) {
                    _env.CurrentShelters.Remove(Shelter);
                    Shelter.tag = "Untagged";
                    Shelter.GetComponent<MeshRenderer>().material = NonSelectMaterial;
                    selectList.Add(false);
                } else {
                    Debug.LogError("Invalid action");
                }
            }

            // 行動ログを記録（episode, step, 各避難所候補の選択状況のリスト(true or false)）
            ActionLogs.Add(new Tuple<int, int, List<bool>>(_env.currentEpisodeId, _env.currentStep, selectList));
            

            OnDidActioned?.Invoke();
        }

        /// <summary>
        /// （比較実験用）
        ///  ニューラルネットワークではなく、ランダムに建物を選択するヒューリスティック関数
        ///  ML-Agentsの訓練モードを起動していない & 学習済みモデルがアタッチされていない場合はこの関数に従い行動します
        ///  https://docs.unity3d.com/Packages/com.unity.ml-agents@3.0/api/Unity.MLAgents.Agent.html#Unity_MLAgents_Agent_Heuristic_Unity_MLAgents_Actuators_ActionBuffers__
        /// </summary>
        public override void Heuristic(in ActionBuffers actionsOut) {
            var Selects = actionsOut.DiscreteActions;

            for(int i = 0; i < Selects.Length; i++) {
                if(Disabled) {
                    Selects[i] = ConstBldgs.Contains(ShelterCandidates[i]) ? 1 : 0;
                } else {
                    Selects[i] = UnityEngine.Random.Range(0, 2);
                }
            }
        }


    }

  ```
  シーン内の`ShelterManagementAgent`オブジェクトにアタッチしてください。

</details>


<details>
<summary>PLATEAU属性情報のJSONデータをデシリアライズするための型定義の作成</summary>

- `CityObjectTypes.cs`
    ```cs
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class AttributeValue
    {
        public string Key { get; set; }
        public string Type { get; set; }

        private object value;
        
        /// <summary>
        /// ゲッター (get) では、Type が "AttributeSet" である場合には AttributeSetValue を返し、それ以外の場合には value を返します。これは、Type に応じて異なる値を返すための条件付きロジックを実装しています。
        /// セッター (set) では、Type が "AttributeSet" であり、かつ value が JArray 型である場合に特別な処理を行います。この場合、value を JArray としてキャストし、それを List<AttributeValue> に変換して AttributeSetValue に設定します。JArray は JSON 配列を表す型であり、これをリストに変換することで、JSON データをオブジェクトのリストとして扱えるようにしています。
        /// </summary>
        [JsonProperty("value")]
        public object Value
        {
            get => Type == "AttributeSet" ? AttributeSetValue : value;
            set
            {
                if (Type == "AttributeSet" && value is JArray jArray)
                {
                    // JSON配列をList<AttributeValue>に変換
                    AttributeSetValue = jArray.ToObject<List<AttributeValue>>();
                }
                else
                {
                    this.value = value;
                }
            }
        }

        // "type": "AttributeSet"の場合にデシリアライズされるプロパティ
        [JsonIgnore] // このプロパティはJSONに含めない
        public List<AttributeValue> AttributeSetValue { get; private set; }
    }

    public class RootObject
    {
        public string GmlID { get; set; }
        public List<int> CityObjectIndex { get; set; }
        public string CityObjectType { get; set; }
        public List<AttributeValue> Attributes { get; set; }
    }


    ```
    後述の`シミュレーション環境制御プログラムの作成`で建物オブジェクトの属性情報取得で使用します。
</details>

<details>
<summary>シミュレーション環境制御プログラムの作成</summary>

- `Utils.cs`
    ```cs
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    public class Utils : MonoBehaviour {
        
        /// <summary>
        /// 避難者のランダムスポーン範囲を描画する
        /// </summary>
        public static void DrawWireCircle(Vector3 center, float radius, int segments = 36) {
            float angle = 0f;
            float angleStep = 360f / segments;

            Vector3 prevPoint = center + new Vector3(radius, 0, 0); // 初期点

            for (int i = 1; i <= segments; i++) {
                angle += angleStep;
                float rad = Mathf.Deg2Rad * angle;

                Vector3 newPoint = center + new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);

                prevPoint = newPoint; // 次の線を描画するために現在の点を更新
            }
        }


        /// <summary> 汎用的なCSV保存関数 </summary>
        public static void SaveResultCSV<T>(string[] header, List<T> dataList, Func<T, string[]> convertToCSVRow, string filePath = null, bool append = true) {

            if (filePath == null) {
                filePath = "result.csv";
            }
            // パスの先頭に指定パスを付与
            filePath = Path.Combine(Application.dataPath, filePath);
            // フォルダが存在しない場合は作成
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            bool writeHeader = !File.Exists(filePath) || !append;
            using (StreamWriter writer = new StreamWriter(filePath, append)) {
                if (writeHeader) writer.WriteLine(string.Join(",", header));

                foreach (T data in dataList) {
                    string[] row = convertToCSVRow(data);
                    writer.WriteLine(string.Join(",", row));
                }
            }
            Debug.Log($"CSV saved: {filePath}");
        }

    }
    ```

- `ShelterEnvManager.cs`
    ```cs
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AI;
    using TMPro;
    using PLATEAU.CityInfo;
    using PLATEAU.Util;
    using Newtonsoft.Json;

    /// <summary>
    /// シミュレータ環境全般の制御を行うクラス
    /// </summary>
    public class EnvManager : MonoBehaviour {
        /**シミュレーションモードの選択を定義*/
        public enum SimulateMode {
            Train, // モデル訓練
            Inference // モデル推論
        }

        public enum SpawnMode {
            Random, // 一定の範囲内でランダムに出現
            Custom, // 自身でスポーン位置・範囲を設定
        }

        [Header("Environment Settings")]
        public SimulateMode Mode = SimulateMode.Train; 
        public SpawnMode EvacSpawnMode = SpawnMode.Random; 
        public float TimeScale = 1.0f; // 推論時のシミュレーションの時間スケール
        public bool IsRecordData = false;
        /// <summary>
        /// 生成する避難者の人数に合わせて避難所の収容人数をスケーリングします.
        /// </summary>
        /// <example>
        /// スケーリング例:
        /// <list type="bullet">
        /// <item>
        /// <description>1.0f: 通常 → 収容人数算出式に合わせて避難所の収容人数を設定</description>
        /// </item>
        /// <item>
        /// <description>0.5f: 避難者の人数が半分 → 避難所の収容人数も半分</description>
        /// </item>
        /// </list>
        /// </example>
        public float AccSimulateScale = 1.0f; 
        public float MaxSeconds = 60.0f; // シミュレーションの最大時間（秒）
        public int SpawnEvacueeSize;
        public GameObject SpawnEvacueePref; // 避難者のプレハブ
        public float SpawnRadius = 10f; // スポーンエリアの半径
        public Vector3 spawnCenter = Vector3.zero; // スポーンエリアの中心位置

        public GameObject AgentObj;
        public ShelterManagementAgent Agent;
        public bool IsDataCollectionMode;

        [Header("Objects")]
        [System.NonSerialized]
        public List<GameObject> Evacuees; // 避難者のリスト
        [System.NonSerialized]
        public List<GameObject> CurrentShelters; // 現在のアクティブな避難所のリスト
        public List<GameObject> Shelters; // 全避難所のリスト

        [Header("UI Elements")]
        public TextMeshProUGUI stepCounter;
        public TextMeshProUGUI evacRateCounter;

        // Event Listeners
        /** エピソード終了時に発行するイベント関数 */
        public delegate void EndEpisodeHandler(float evacueeRate);
        public EndEpisodeHandler OnEndEpisode;
        /**エピソード開始時に発行するイベント関数 */
        public delegate void StartEpisodeHandler();
        public StartEpisodeHandler OnStartEpisode;
        [Header("Parameters")]
        public float EvacuationRate; // 全体の避難率
        public bool EnableEnv = false; // 環境の準備が完了したか否か（利用不可の場合はfalse）
        public int currentStep; // 現在のステップ数
        private float currentTimeSec; //現在の経過時間（秒）
        private List<Tuple<float, float>> evaRatePerSec = new List<Tuple<float, float>>(); // 避難率の時間変化を記録するリスト
        public int currentEpisodeId = 0; // エピソード番号
        public string recordID; // データ記録用に実行時間を元にしたIDを生成

        void Start() {
            if(Mode == SimulateMode.Inference) {
                Time.timeScale = TimeScale; // 推論時のみシミュレーションの時間スケールを設定
            }

            if(AccSimulateScale > 1.0f) {
                Debug.LogError("AccSimulateScale is greater than 1.0f. Please set the value between 0.0f and 1.0f.");
            }
            // 日付-時間-分-秒を組み合わせた記録用IDを生成
            recordID = System.DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss");

            NavMesh.pathfindingIterationsPerFrame = 1000000; // パス検索の上限値を設定

            Agent = AgentObj.GetComponent<ShelterManagementAgent>();
            Evacuees = new List<GameObject>(); // 避難者のリストを初期化
            CurrentShelters = new List<GameObject>(); // 避難所のリストを初期化
            Shelters = new List<GameObject>(); // 避難所のリストを初期化
            currentStep = Agent.StepCount;

            if(IsDataCollectionMode) {
                Agent.Disabled = true;
            }

            // 避難所登録
            Shelters = new List<GameObject>(GameObject.FindGameObjectsWithTag("Shelter"));
            // 固定値の避難所を追加
            GameObject[] constSheleters = GameObject.FindGameObjectsWithTag("ConstShelter");
            foreach (var shelter in constSheleters) {
                Shelters.Add(shelter);
            }
            // コンポーネントの初期化
            foreach (var shelter in Shelters) {
                if(shelter.GetComponent<Shelter>() == null) {
                    Shelter tower = shelter.AddComponent<Shelter>();
                    tower.uuid = System.Guid.NewGuid().ToString();
                    tower.MaxCapacity = GetAccSize(shelter);
                    tower.NowAccCount = 0;
                }
            }

            /** エピソード終了時の処理*/
            OnEndEpisode += OnEndEpisodeHandler;
        }

        // エディタ上で、避難者の生成範囲を赤円で示す
        void OnDrawGizmos() {
            if(EvacSpawnMode == SpawnMode.Random) {
                Gizmos.color = Color.red;
                DrawWireCircle(spawnCenter, SpawnRadius);
            }
        }

        // 避難率の更新や、経過時間等シミュレーションの更新処理
        void FixedUpdate() {
            currentTimeSec += Time.deltaTime;
            EvacuationRate = GetCurrentEvacueeRate();
            evaRatePerSec.Add(new Tuple<float, float>(currentTimeSec, EvacuationRate));
            UpdateUI();
            if (currentTimeSec >= MaxSeconds || IsEvacuatedAll()) { // 制限時間 or 全避難者が避難完了した場合
                OnEndEpisode?.Invoke(EvacuationRate); // 制限時間を超えた場合、エピソード終了のイベントを発火
            }
        }

        /// エピソード終了時に実行する。データの保存。
        private void OnEndEpisodeHandler(float evacuateRate) {
            // 1. 避難率による報酬
            float evacuationRateReward = GetCurrentEvacueeRate();

            // 2. 経過時間によりボーナスを与える
            float timeBonus = (MaxSeconds - currentTimeSec) / MaxSeconds;

            // 総合報酬
            float totalReward = evacuationRateReward + timeBonus;
            Debug.Log("Total Reward: " + totalReward);
            Agent.AddReward(totalReward);

            if(IsRecordData) {
                Utils.SaveResultCSV(
                    new string[] { "Time", "EvacuationRate" }, 
                    evaRatePerSec, 
                    (data) => new string[] { data.Item1.ToString(), data.Item2.ToString() },
                    $"{recordID}/EvaRatesPerSec_Episode_{currentEpisodeId}.csv"
                );
            }

            /**エピソード終了の発行*/
            Agent.OnEndEpisode();
            Agent.EndEpisode();
            currentEpisodeId++;
        }

        /// <summary>
        /// エピソード開始時の初期化処理
        /// この関数はエージェントのイベント関数から参照されます 
        /// </summary>
        public void OnEpisodeBegin() {
            EnableEnv = false;
            Dispose();
            Create();
            OnStartEpisode?.Invoke();
            EnableEnv = true;
        }

        /// <summary>
        /// 環境をリセット,破棄をする関数。
        /// - 避難者のクリア
        /// - 避難所のクリア
        /// </summary>
        public void Dispose() {
            foreach (var evacuee in Evacuees) {
                Destroy(evacuee);
            }
            // 避難者スポーン地点の表示を非表示にする
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPos");
            foreach (var spawnPoint in spawnPoints) {
                var point = spawnPoint.GetComponent<EvacueeSpawnPoint>();
                point.ShowRangeOff();
            }
            Evacuees = new List<GameObject>(); // 新しいリストを作成
            CurrentShelters = new List<GameObject>(); // 新しいリストを作成
            currentTimeSec = 0;
            evaRatePerSec.Clear();
        }

        /// <summary>
        /// 環境の生成を行う関数.
        /// - 避難者のスポーン 処理
        /// </summary>
        public void Create() {

            if(Mode == SimulateMode.Train) {
                if(EvacSpawnMode == SpawnMode.Custom) {
                    // Custom Spawnエリアの中からランダムに1つ選択し、避難者をスポーンさせ、避難者位置に分布を持たせる
                    GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPos");
                    GameObject selectSpawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                    var point = selectSpawnPoint.GetComponent<EvacueeSpawnPoint>();
                    point.ShowRangeOn();
                    float radius = point.SpawnRadius;
                    Vector3 spawnCenter = selectSpawnPoint.transform.position;
                    // 生成ポイントを中心としたランダムなナビメッシュ上の位置を取得
                    Vector3 spawnPos = GetRandomPositionOnNavMesh(radius, spawnCenter);
                    for (int i = 0; i < SpawnEvacueeSize; i++) {
                        SpawnEvacuee(spawnPos);
                    }
                } else {
                    for (int i = 0; i < SpawnEvacueeSize; i++) {
                        Vector3 spawnPos = GetRandomPositionOnNavMesh(SpawnRadius, spawnCenter);
                        if (spawnPos != Vector3.zero) {
                            SpawnEvacuee(spawnPos);
                        }
                    }
                }
                

            } else if(Mode == SimulateMode.Inference) {
                if(EvacSpawnMode == SpawnMode.Custom) {
                    GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPos");
                    foreach (var spawnPoint in spawnPoints) {
                        var point = spawnPoint.GetComponent<EvacueeSpawnPoint>();
                        float radius = point.SpawnRadius;
                        Vector3 spawnCenter = spawnPoint.transform.position;
                        Vector3 spawnPos = GetRandomPositionOnNavMesh(radius, spawnCenter);
                        for (int i = 0; i < point.SpawnSize; i++) {
                            SpawnEvacuee(spawnPos);
                        }
                    }
                } else {
                    for (int i = 0; i < SpawnEvacueeSize; i++) {
                        Vector3 spawnPos = GetRandomPositionOnNavMesh(SpawnRadius, spawnCenter);
                        if (spawnPos != Vector3.zero) {
                            SpawnEvacuee(spawnPos);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 避難者１体を生成、登録する関数
        /// </summary>
        /// <param name="spawnPos"></param>
        private void SpawnEvacuee(Vector3 spawnPos) {
            GameObject evacuee = Instantiate(SpawnEvacueePref, spawnPos, Quaternion.identity, transform);
            evacuee.tag = "Evacuee";
            Evacuees.Add(evacuee);
        }

        /// <summary>
        /// 範囲内のナビメッシュ上の任意の座標を取得する。
        /// </summary>
        /// <returns>ランダムなナビメッシュ上の座標 or Vector3.zero</returns>
        private static Vector3 GetRandomPositionOnNavMesh(float radius, Vector3 center) {
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius; // 半径内のランダムな位置を取得
            randomDirection += center; // 中心位置を加算
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas)) {
                return hit.position;
            }
            return Vector3.zero; // ナビメッシュが見つからなかった場合
        }

        private void UpdateUI() {
            stepCounter.text = $"Remain Seconds : {MaxSeconds - currentTimeSec:F2}";
            evacRateCounter.text = $"Evacuation Rate : {EvacuationRate:F2}";
        }

        /// <summary>
        /// 現在の避難完了率を取得する
        /// </summary>
        /// <returns>現在の避難完了率: 0～1</returns>
        private float GetCurrentEvacueeRate() {
            int evacueeSize = Evacuees.Count;
            int evacuatedSize = 0;
            foreach (var evacuee in Evacuees) {
                if (!evacuee.activeSelf) {
                    evacuatedSize++;
                }
            }
            return (float)evacuatedSize / evacueeSize;
        }



        /// <summary>
        /// 避難者のランダムスポーン範囲を描画する
        /// </summary>
        private static void DrawWireCircle(Vector3 center, float radius, int segments = 36) {
            float angle = 0f;
            float angleStep = 360f / segments;

            Vector3 prevPoint = center + new Vector3(radius, 0, 0); // 初期点

            for (int i = 1; i <= segments; i++) {
                angle += angleStep;
                float rad = Mathf.Deg2Rad * angle;

                Vector3 newPoint = center + new Vector3(Mathf.Cos(rad) * radius, 5, Mathf.Sin(rad) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);

                prevPoint = newPoint; // 次の線を描画するために現在の点を更新
            }
        }


        /// <summary>
        /// 属性情報から避難所の収容人数を取得する
        /// 【計算式】
        /// 収容可能人数＝ 床総面積㎡×0.8÷1.65㎡
        /// ※出典：https://manboukama.ldblog.jp/archives/50540532.html
        /// </summary>
        /// <param name="shelterBldg">避難所のGameObject</param>
        /// <returns>避難所の収容人数(設定パラメータによりスケーリングされます)</returns>
        private int GetAccSize(GameObject shelterBldg) {
            double? totalFloorSize = null;
            // PLATEAU City Objectから、建物の高さを取得し、避難所の収容人数を動的に設定する
            var cityObjectGroup = shelterBldg.GetComponent<PLATEAUCityObjectGroup>();
            var rootCityObject = cityObjectGroup.CityObjects.rootCityObjects[0];

            // Newtonsoft.Jsonを使用して、CityObjectの属性情報クラスにデシリアライズして取得
            var cityObjectJsonStr = JsonConvert.SerializeObject(rootCityObject);
            var attributes = JsonConvert.DeserializeObject<RootObject>(cityObjectJsonStr).Attributes;
            // 属性値リストを巡回し、床総面積から収容人数を算出
            foreach(var attribute in attributes) {
                if(attribute.Key == "uro:buildingDetailAttribute") {
                    foreach(var uroAttr in attribute.AttributeSetValue) { 
                        if(uroAttr.Key == "uro:totalFloorArea") {
                            if(double.TryParse(uroAttr.Value.ToString(), out double parsedValue)) {
                                totalFloorSize = parsedValue;
                            }
                        }
                    }
                }
            }

            // 結果が取得できなかった場合は0を返す
            if(totalFloorSize == null) {
                Debug.LogError("Failed to get the total floor size of the shelter building.");
                return 0;
            } else {
                // 収容可能人数＝総面積×0.8÷1.65㎡とする
                return (int)((totalFloorSize * 0.8 / 1.65) * AccSimulateScale);
            }
        }


        private bool IsEvacuatedAll() {
            foreach (var evacuee in Evacuees) {
                if (evacuee.activeSelf) {
                    return false;
                }
            }
            return true;
        }
    }

    ```
    作成後シーン内の`Field`オブジェクトにアタッチしてください。
</details>


## 7. シミュレーション環境パラメータの設定

### 6-1. `ShelterManagementAgent`の設定
AIの挙動を制御する`ShelterManagementAgent`の設定を行います。Inspectorから`Behavior Parameters`の以下項目を設定します。
- `Behavior Parameters`
```
Actions
    Continuous Actions : 0
    Discrete Branches : 9（避難所の数だけ）
        Branch 0 Size : 2 （二値分類の為）
        ...(以下候補地数分)
```
![alt text](image-27.png)

今回エピソード終了は、次の全体制御用プログラムで行っているため、`Max Steps`は`0`のままで設定してください。

### 6-2. `ShelterEnvManager`の設定
シミュレーション全体の条件を設定する`ShelterEnvManager`の設定を行います。Inspectorから以下の項目を設定します。
- `ShelterEnvManager`
```
<Environment Settings>
Mode               : Train / Inference （シミュレーションモード 訓練 / 推論 の選択）
Evac Spawn Mode    : Random / Custom （避難者の生成方法の指定 ランダム / カスタム の選択）
Acc Simulate Scale : 0.01 （避難所の収容人数のスケーリング係数。生成する避難者数に合わせて適宜調節） 
Max Seconds     : 1800 （シミュレーションの最大時間（秒））
Spawn Evacuee Size : 50 （生成する避難者の数）// 数が多いと処理が重くなるため、適宜調整してください。
Spawn Radius : 10 （避難者のスポーンエリアの半径）// エディタ上に赤い円で表示されます
Spawn Center : (0,0,0) （避難者のスポーンエリアの中心位置）
Agent : ShelterManagementAgent （エージェントのオブジェクト）
```

#### 6-2-1. 避難者生成モードについて

![alt text](image-23.png)

- `Random` : 避難者を設定半径内の道路上にランダム生成します。避難者のスポーンエリアは`Spawn Radius`で指定した範囲内にランダムに生成されます。
```
Spawn Radius : 10 （避難者のスポーンエリアの半径）// エディタ上に赤い円で表示されます
```
ここで設定した半径に基づいて避難者生成が行われます。広域な範囲で満遍なく避難者を生成したい場合は、このモードを利用します。

- `Custom` : 避難者を特定の地点でスポーンさせたい場合に使用します。避難者のスポーン中心地点は`SpawnPoint`オブジェクトの位置になります。この時の避難者生成条件は`EvacueeSpawnPoint`の設定値になります。

このモードは、特定の地域や複数地域設定することができ、地域ごとに避難者の生成条件を変えたい場合に使用します。
実行時には、設定された生成ポイントの内、１つがランダムに選択されシミュレーションが実行されます。１回のシミュレーション終了後には、再度ランダムに生成ポイントが１つ選択されます。
```
Evacuee Prefab : 避難者のプレハブ
Spawn Radius : 10 （避難者の生成半径）
Spawn Size : 50 （避難者の生成人数）
```
今回はこのカスタムモードを使って、シミュレーションを実行していきます。
![alt text](image-28.png)


### 6-3. `Evacuee`の設定
避難者の挙動を制御する`Evacuee`の設定を行います。Assets内から`Evacuee`オブジェクトを選択し、Inspectorから以下の項目を設定します。設定値は任意です。
- `NavMesh Agent`
```
<Steering>
Speed : 3.5 （避難者の移動速度 m/s）
Angular Speed : 120 （避難者の回転速度）
```
![alt text](image-17.png)


### 6-4. ハイパーパラメータの設定
プロジェクト内の任意の場所に、ニューラルネットワークのハイパーパラメータの値を記載する
yamlファイルを作成します。

```yaml
behaviors:
  ShelterSelect:
    trainer_type: ppo # 使用するトレーナーの種類（ここではProximal Policy Optimization）
    hyperparameters:
      batch_size: 512 # 一度にトレーニングするサンプルの数
      buffer_size: 409600 # 経験バッファのサイズ
      learning_rate: 1e-3 # 学習率
      beta: 0.01 # エントロピー正則化の強さ
      epsilon: 0.3 # クリッピング範囲のパラメータ
      lambd: 0.99 # GAE（Generalized Advantage Estimation）のパラメータ
      num_epoch: 5 # 各バッチのトレーニングエポック数
      learning_rate_schedule: linear # 学習率のスケジュール（ここでは線形減衰）
    network_settings:
      normalize: false # 入力データの正規化を行うかどうか
      hidden_units: 512 # 各隠れ層のユニット数
      num_layers: 3 # 隠れ層の数
      vis_encode_type: simple # 視覚エンコーダのタイプ
    reward_signals:
      extrinsic:
        gamma: 0.80 # 割引率
        strength: 1.0 # 報酬信号の強さ
    keep_checkpoints: 5 # 保存するチェックポイントの数
    max_steps: 500000 # トレーニングの最大ステップ数
    time_horizon: 2048 # エージェントの時間的ホライゾン
    summary_freq: 5 # サマリーを記録する頻度
```
- 利用可能なパラメータの値については[公式ドキュメント](https://github.com/Unity-Technologies/ml-agents/blob/develop/docs/Training-Configuration-File.md)を参照してください。

<details>

<summary>パラメータ設定のポイント</summary>

- ハイパーパラメータの組み合わせを変えて学習を行った際の累積報酬の結果のグラフ
![alt text](image-24.png)

各種パラメータの値については、[8-2. 学習結果の分析](#8-2-学習結果の分析)でTensorBoardを使用して学習の進捗を確認し、適切な値を設定していきますが、大まかな値の調整については、累積報酬が安定的に増加しているか、エントロピーの大小, 減少具合により決定していきます。

以下、今回のシミュレーションで作成したサンプルモデルに関する値の調整について記述します。

#### `trainer_type : ppoまたはsac`

利用する強化学習アルゴリズムの決定を行う設定。

パラメータを複数検証した結果、エントロピーの大きさはさほど変わらなかったが、ppoでの方が累積報酬が高く安定していたことが決め手。sacだと、累積報酬が不安定になった。


#### `buffer_size : 一般的な値：2048 - 409600 (ppoの場合)`

モデルを更新する前に収集すべき経験数。 モデルの学習や更新を行う前に収集すべき経験数に相当する。通常`buffer_size`が大きいほど、より安定した学習につながる。

値を、`10240, 12000,  409600`の複数条件で学習させた結果、`409600`が一番累積報酬が高く、安定していたのが決め手。
※後述の`batch_size`の値により変化する可能性あり。

#### `batch_size：一般的な値：32 - 512（離散行動でppoの場合）`

勾配降下の各反復における経験値数。 
これは常に`buffer_size`の数倍小さくなければならない。

今回は、`2024, 3024, 512`の組み合わせで試行した結果、`512`が一番累積報酬が高く安定的であったためこれを選択。

#### `beta：一般的な値：0.0001～0.01`

エージェントが訓練中にランダムな行動選択を行う強さ。実際にシミュレーションを行いその結果で報酬を獲得するという強化学習の特性上、訓練中は、様々な行動選択の組み合わせを試し経験を積むことが望ましい（そこから報酬が最大化する法則を導き出す）。TensorBoardのグラフから、累積報酬の増加とともに、エントロピーの大きさがゆっくりと下降するようになる値が望ましい。

値を、`0.001, 0.005, 0.01`で学習させた結果、`0.01`が最も低いエントロピーの値を示しかつ累積報酬の安定が見られたため0.01に設定。

#### `network_settings > normalize`

観測の入力を正規化するか否かの設定。今回の場合では、避難所の位置座標や避難者の位置座標などモデルへの入力を正規化するか否かを設定する値。

エージェントの行動空間が連続値をとり、かつ、複雑なタスク（ロボットの歩行制御などの複雑な動き）の場合には学習を収束させるのに効果的であるが、今回の様に単純な離散値だけを行動空間にとる場合はむしろ学習を発散させる傾向があるため今回のような単純な選択問題の場合は`false（正規化しない）`にすることが推奨される。

`false（正規化しない）, true（正規化する）`の両設定を試した結果、`false`の方が累積報酬が安定的かつエントロピーも低い値であったため、学習の収束があったと判断し、`false`を採用。

#### `network_settings > hidden_units` : 一般的な値 : 32～512

モデルのニューラルネットワークの隠れ層のノード数。一般に複雑なタスクにおいては値を大きくすることが求められるが、簡単なタスクにおいては比較的小さい値に設定することが望ましい。

今回は、`128, 666, 512`の３つの値の組み合わせの内、`512`が最も累積報酬が高く安定したためこの値を採用。

#### `network_settings >num_layers` : 一般的な値 : 1～4

モデルのニューラルネットワークの隠れ層の数。
一般に複雑なタスクではこの値は大きくする必要があるが、簡単なタスクにおいては小さくすることが望ましい。

今回は、`2,3,4`の３つの値の組み合わせの内、`2`が最も累積報酬が安定したためこの値を採用。

</details>

## 8. 学習の実行と結果の確認

### 8-1. 学習の実行
本プロジェクトのルートディレクトリ上でターミナルを開き、以下のコマンドを実行します。
``` bash
mlagents-learn Assets/Config/Tutorial-1.yaml --run-id=ShelterAgent
```
準備が完了すると、以下のような表示になり待機状態になります。

![alt text](../Common/image-22.png)

この状態で、Unity Editor上の実行ボタンを押すと、学習が開始されます。

![alt text](../Common/image-23.png)

### 8-2. 学習結果の分析

学習が完了すると、`results`ディレクトリに学習した結果のニューラルネットワークモデルが保存されます。学習結果を確認するには、以下のコマンドを実行します。
``` bash
tensorboard --logdir=./results
```
![alt text](../Common/image-24.png)

### 8-3. 学習済みモデルを使用してシミュレーションを動かす
学習済みモデルを使用して、シミュレーションを動かす手順は以下の通りです。

1. エージェントにモデルを割り当てる
    学習済みモデルをAssetsフォルダにコピーし、シーン内の`ShelterSelectAgent`を選択し`Behavior Parameters`の`Model`にコピーしたモデルを割り当てます。

    ![alt text](image-18.png)

2. シーンを実行する
    Unity Editor上で実行ボタンを押すと、学習済みモデルを使用してシミュレーションが動きます。

    ![alt text](../Common/image-23.png)

    エージェントはシミュレーション開始時に、都市内の避難者の分布や建物の収容人数を観測し、避難所の配置を決定します。その後、避難者が避難所に向かう様子を観察することができます。

3. シミュレーション結果を分析する。
    今回作成したAIモデルのシミュレーション結果の分析は、[「チュートリアル① モデル分析編」](https://github.com/Rikkyo-MiyayuLab/PLATEAU-Tutorial/tree/develop/docs/Tutorial-1-Inference)を参照してください。

