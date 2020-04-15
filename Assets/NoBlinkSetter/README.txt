***NoBlinkSetter (ver1.0.1)***
まばたきと表情が干渉するのを防ぐためのアバターギミックを自動で設定するツールです。

〇はじめに
元怒さん(@gend_VRchat)が考案したアバターギミックをgatosyocoraが自動設定するツールとして作成しました。
Animatorを使ってまばたきをするアバターにのみ対応しています。
本ツールではVRCSDKを使用してるのでVRCSDKをインポートしてから本ツールをインポートしてください

〇内容物
 - Editor : 必要なスクリプトが入ったフォルダです
   - NoBlinkSetter.cs : アバターに本アバターギミックを設定するEditor拡張
   - NoBlinkKeyAdder.cs : Animationファイルにまばたき防止Animatorを操作するキーを追加するEditor拡張
   - NoBlinkKeyCopier.cs : Animationファイルにまばたき防止Animatorを操作するキーをコピーするスクリプト
 - Animations : 自動生成されたAnimationファイルなどが入るフォルダ(中に入っているDummy.animは消して大丈夫です)
 - OriginFiles : スクリプトで使用するファイルが入ったフォルダ
 - LICENSE.txt : ライセンス情報を記載したテキストファイル
 - README.txt : このファイル


Unityのメニュー(FileやVRChatSDK等が並んでいるところ)にある「VRCDeveloperTool」を押して
「NoBlinkSetter」を選択することで以下の機能説明のものが使えます

〇機能説明
 - Avatar : VRC_AvatarDescriptorがついたGameObjectを設定します
 - Standing Anims : Avatarに設定されてあるAnimatorOverrideControllerです
 - Face Mesh : Avatarの表情のBlendShapeがついたメッシュオブジェクトです
 - AnimationClips : Standing AnimsのControllerに設定されているAnimationClipです

 〇使い方
 ・アバターギミックの自動設定
 1. Unityのメニュー(FileやVRChatSDK等が並んでいるところ)にある「VRCDeveloperTool」を押して「NoBlinkSetter」を選択します
 2. Avatarにアバターギミックを設定したいアバターを設定します(VRC_AvatarDescriptorがついたGameobject)
 3. Standing AnimsとFaceMeshが設定されていることを確認します(設定されていなければ適切なものを設定してください)
 4. AnimationClipsにアニメーションオーバーライドで使用するAnimationファイルが設定されていることを確認する
 5. アイトラ対応アバターの場合,「EyeTracking対応アバター」にチェックを入れます
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

〇更新履歴
ver1.0 NoBlinkSetterを作成
ver1.0.1 NoBlink設定時に複製してから設定するように変更
ver1.1 アイトラ対応アバターの場合、まばたき防止の設定後もアイトラできるように

----------------------------------------------------
●利用規約
本規約は本商品に含まれるすべてのスクリプトおよびファイルに共通で適用されるものとする。
本商品を使用したことによって生じた問題に関しては元怒およびgatosyocora(以下, 作者ら)は一切の責任を負わない。

・スクリプト
本スクリプトはzlibライセンスで運用される。
著作権はgatosyocoraに帰属する。

・Animationファイル
同封されているAnimationファイル(OriginFilesの中身)およびスクリプトで生成されるAnimationファイル(Animationsの中身)は
パラメータの一部を含め、商用利用・改変・二次配布を許可する。
その際には作者名や配布元等は記載しなくてもよい。
しかし、本Animationファイルの使用や配布により生じた問題等に関しては作者らは一切の責任を負わない。

-----------------------------------------------------
ギミックに関する質問は元怒(Twitter: @gend_VRchat)まで
エディタ拡張に関する質問・要望はgatosyocora(Twitter: @gatosyocora)まで