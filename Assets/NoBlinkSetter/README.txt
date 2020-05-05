***NoBlinkSetter (ver2.0)***
まばたきと表情が干渉するのを防ぐためのアバターギミックを自動で設定するツールです。
また、一定時間表情切り替えがおこなわれなかったときに寝た表情のAFK状態になるAFK機構を設定できます。

〇はじめに
元怒さん(@gend_VRchat)が考案したアバターギミックをgatosyocoraが自動設定するツールとして作成しました。
Animatorを使ってまばたきをするアバターにのみ対応しています。
本ツールではVRCSDK2を使用してるのでVRCSDK2をインポートしてから本ツールをインポートしてください

〇内容物
 - Editor : 必要なスクリプトが入ったフォルダです
   - NoBlinkSetter.cs : アバターに本アバターギミックを設定するEditor拡張
   - NoBlinkKeyAdder.cs : Animationファイルにまばたき防止Animatorを操作するキーを追加するEditor拡張
   - NoBlinkKeyCopier.cs : Animationファイルにまばたき防止Animatorを操作するキーをコピーするスクリプト
   - GatoEditorUtility.cs : gatosyocoraが作成したEditor拡張用の便利関数群
 - OriginFiles : スクリプトで使用するアセットが入ったフォルダ
 - AFK Systetm : AFK機構に関連したアセットが含まれるフォルダ
 - LICENSE.txt : ライセンス情報を記載したテキストファイル
 - README.txt : このファイル


Unityのメニュー(FileやVRChatSDK等が並んでいるところ)にある「VRCDeveloperTool」を押して
「NoBlinkSetter」を選択することで以下の機能説明のものが使えます

〇機能説明
 - Avatar : VRC_AvatarDescriptorがついたGameObjectを設定します
 - Standing Anims : Avatarに設定されてあるAnimatorOverrideControllerです
 - AnimationClips(Standing Anims) : Standing AnimsのControllerに設定されているAnimationClipです
 - Blink : まばたき関連の項目です。
	 - Face Mesh : Avatarの表情のBlendShapeがついたメッシュオブジェクトです
	 - BlendShape : Avatarのまばたき用のBlendShapeです。「+」「-」で項目数を増やします。
					項目数が1で未選択の場合BlinkAnimationから自動取得するボタンが表示されます
	 - BlinkController : まばたきアニメーションが設定されたAnimatorControllerです
	 - BlinkAnimation : まばたきアニメーションのAnimationClipです。
						未設定の場合、まばたきアニメーションの自動作成ボタンが表示されます
 - AFK System : AFK機構関連の項目です
	 - AFKになるまでの時間(分) : 表情が変更されていない状態がこの時間続くとAFK状態になります
	 - AFK中のエフェクト : AFK状態のときに表示されるエフェクトです。CUSTOMを選択すると好きなオブジェクトを設定できるようになります
	 - AFK中に表示するObject : AFK中のエフェクトでCUSTOMを選択した場合、表示されるAFKエフェクトです
	 - AFKエフェクトの接続先 : 上記のAFK中のエフェクトをParent Constraintを用いて接続する際の接続先です
 - SaveFolder : 本ツールで作成されるアセットの保存先です。
				デフォルトではStanding Animsに設定されたAnimatorOverrideControllerの場所に「NoBlink」というフォルダを作成し設定しています。

 〇使い方
 ・アバターギミックの自動設定
 1. Unityのメニュー(FileやVRChatSDK等が並んでいるところ)にある「VRCDeveloperTool」を押して「NoBlinkSetter」を選択します
 2. Avatarにアバターギミックを設定したいアバターを設定します(VRC_AvatarDescriptorがついたGameobject)
 3. Standing AnimsとBlinkFaceMesh,Blink BlendShape, BlinkController, BlinkAnimationが設定されていることを確認します
	(設定されていなければ適切なものを設定してください)
 4. AnimationClipsにアニメーションオーバーライドで使用するAnimationファイルが設定されていることを確認する
 5. AFK機構を設定する場合はAFK Systemにチェックを入れ、AFK機構に関する項目を設定する
 6. 「Set NoBlink」を選択する
 ※4で設定されているAnimationファイルは複製され, 自動的にまばたき防止Animatorを操作するキーの追加およびパスの修正がおこなわれます。
 よってアバターギミックによって壊れて修正不可になることはありません

 ・アバターギミック設定後の表情切り替え用Animationファイルの作成方法
 （まばたき防止Animatorを操作するキーを新しく作成したAnimationファイルに追加する方法）
  1. 通常の表情作成と同様の方法でAnimationファイルの作成とBlendShapeキーの設定をおこなう
  2. 設定したAnimationファイルをProjectタブから選択し, Inspectorを確認する
  3. 上部のAnimationファイルの名前が表示されているところあたりを右クリックする
  4. FISTに設定予定のAnimationファイルであれば「Add NoBlink Key For FIST」を、それ以外であれば「Add NoBlink key」を選択する
  5. まばたき防止Animatorを操作するキーが追加される

  ・まばたき防止Animatorを操作するキーをAnimationファイルから削除する
  1. まばたき防止Animatorを操作するキーを消したいAnimationファイルをProjectタブから選択し, Inspectorを確認する
  3. 上部のAnimationファイルの名前が表示されているところあたりを右クリックする
  4. 「Clear NoBlink Key」を選択する
  5. まばたき防止Animatorを操作するキーが削除される

  ・AFK機構の追加
  1. まばたき防止機構を設定済みまたは未設定なアバターをAvatarに設定
  2. AFK Systemにチェックを入れる
  3. AFK Systemに関する項目を設定する
  4. 「Set NoBlink」を選択する

〇更新履歴
ver1.0 NoBlinkSetterを作成
ver1.0.1 NoBlink設定時に複製してから設定するように変更
ver1.1 アイトラ対応アバターの場合、まばたき防止の設定後もアイトラできるように
ver2.0	・AFK機構を追加
		・作成されるアセットの保存先をNoBlinkSetterフォルダ外に変更
		・まばたきアニメーション作成機能を追加
		・まばたきアニメーションの最適化機能を追加
		・UIを一部変更
		・アイトラッキング対応アバターか自動検出する機能を追加
		・ギミック設定済みアバターか自動検出する機能を追加

〇注意事項
・他のアバターに依存関係がありそうなBlinkControllerやBlinkAnimation, 表情用アニメーションは複製して設定しています。
・まばたき防止機構対応の表情アニメーション(○○_blink reset.anim)作成時にBlinkBlendShapeに設定されたまばたきBlendShapeを操作するアニメーションキーは削除されます。
・まばたき防止機構やAFK機構ではまばたきアニメーションは3秒から開始されないといけないため、それ以内にアニメーションキーがある場合全体を3秒ずらしています。

----------------------------------------------------
●利用規約
本規約は本商品に含まれるすべてのスクリプトおよびファイルに共通で適用されるものとする。
本商品を使用したことによって生じた問題に関しては元怒およびgatosyocora(以下, 作者ら)は一切の責任を負わない。

・スクリプト
本スクリプトはzlibライセンスで運用される。
ただし、GatoEditorUtilityに関してはMITライセンスで運用されている。
著作権はgatosyocoraに帰属する。

・Animationファイル
同封されているAnimationファイル(OriginFilesの中身)およびスクリプトで生成されるAnimationファイル(Animationsの中身)は
パラメータの一部を含め、商用利用・改変・二次配布を許可する。
その際には作者名や配布元等は記載しなくてもよい。
しかし、本Animationファイルの使用や配布により生じた問題等に関しては作者らは一切の責任を負わない。

-----------------------------------------------------
ギミックに関する質問は元怒(Twitter: @gend_VRchat)まで
エディタ拡張に関する質問・要望はgatosyocora(Twitter: @gatosyocora)まで