﻿module TsuyoGame

open System
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open TsuyoTsuyo

type TsuyoGame() as this =
  inherit Game()

  let gameTitle = "つよつよえふしゃ～ぷ！"
  let tsuyoWidth,tsuyoHeight = 48,48
  let graphicsDeviceManager = new GraphicsDeviceManager(this)
  let sprite = lazy new SpriteBatch(this.GraphicsDevice)
  let font = lazy this.Content.Load<SpriteFont>("Font")

  let fps = 60.

  let mutable ps = createTsuyoObj twitStatusList

  let mutable textures:(string * Lazy<Texture2D>) list = ["##dummy##",lazy this.Content.Load("480_16colors_normal")]

  let drawTsuyo (tsuyo:Tsuyo) =
    let fx,fy = float32 (tsuyo.Pos%RowNum*tsuyoWidth), float32 (tsuyo.Pos/RowNum*tsuyoHeight)
    textures |> List.tryPick (fun (k,v) -> if k = tsuyo.ScreenName then Some (k,v) else None) |> function
      | Some (k,v) -> sprite.Force().Draw(v.Force(), Vector2(fx,fy), Color.White)
      | None -> 
        // tsuyo.ImageStreamがSomeの場合しかこのstatementに到達しないので、Option.ValueでStreamを取得している
        let texture = lazy Texture2D.FromStream(this.GraphicsDevice, tsuyo.ImageStream.Value)
        sprite.Force().Draw(texture.Force(), Vector2(fx,fy), Color.White)
        textures <- (tsuyo.ScreenName ,texture)::textures
      
  let operateKeys () =
    let operateKey =
      function
      | Keys.Z -> rotateL ps
      | Keys.X -> rotateR ps
      | Keys.Left ->
        ps |> function
          | CollideLeftWall -> ps
          | _ -> if detectCollision (ps.Tsuyo1.Pos-1) || detectCollision (ps.Tsuyo2.Pos - 1) then ps else move ps Direction.Left
      | Keys.Right ->
        ps |> function
          | CollideRightWall -> ps
          | _ -> if detectCollision (ps.Tsuyo1.Pos+1) || detectCollision (ps.Tsuyo2.Pos + 1) then ps else move ps Direction.Right
      | Keys.Down ->
        move ps Direction.Down
      | _ -> ps
      
    Keyboard.GetState().GetPressedKeys()
    |> Array.toList
    |> List.map operateKey |> function | [] -> ps.Tsuyo1,ps.Tsuyo2 | list -> list |> List.head |> (fun (x:TsuyoObj) -> x.Tsuyo1,x.Tsuyo2)

  let eraseTsuyo (tsuyo:Tsuyo) =
    fieldTsuyo <- tsuyo::fieldTsuyo
    let union = getUnion tsuyo.ScreenName tsuyo.Pos
    if List.length union >= EraseNum then union |> erase |> fun list -> fieldTsuyo <- list 

  do
    this.Content.RootDirectory <- "TsuyoTsuyoContent"
    this.Window.Title <- gameTitle
    (800,600) ||>
      fun x y ->
        graphicsDeviceManager.PreferredBackBufferWidth <- x
        graphicsDeviceManager.PreferredBackBufferHeight <- y
    1. / fps |> fun sec -> this.TargetElapsedTime <- TimeSpan.FromSeconds sec

  override game.Initialize () =
    graphicsDeviceManager.GraphicsProfile <- GraphicsProfile.HiDef
    graphicsDeviceManager.ApplyChanges()
    base.Initialize()

  override game.BeginRun () = async { start () } |> Async.Start

  override game.Update gameTime =
    operateKeys ()
    |> fun (x:Tsuyo,y:Tsuyo) ->
      if detectCollision (x.Pos+RowNum) || detectCollision (y.Pos+RowNum) then
        ps.Tsuyo2Pos |> function
          | SndTsuyoPos.Right | SndTsuyoPos.Left -> (x,y) ||> fun x y -> (fall x), (fall y)
          | _ -> x,y
        |> fun (x,y) -> [x;y] |> List.iter eraseTsuyo; ps <- createTsuyoObj twitStatusList
      else
        ps |> function
          | CollideBottom -> [x;y] |> List.iter eraseTsuyo; ps <- createTsuyoObj twitStatusList
          | _ -> ()
    base.Update gameTime

  override game.Draw gameTime =
    sprite.Force().Begin()
    ps.Tsuyo1 :: ps.Tsuyo2 :: fieldTsuyo |> List.filter (fun (x:Tsuyo) -> x.Hidden |> not) |> List.map drawTsuyo |> ignore
    twitStatusList |> List.filter (fun x -> x.IsSome)
    |> List.map (fun x -> sprite.Force().DrawString(font.Force(),x.Value.User.ScreenName,Vector2(300.f,300.f),Color.White)) |> ignore
    sprite.Force().End()
    base.Draw gameTime

  override game.EndRun () =
    textures |> List.iter (fun (_,v) -> v.Force().Dispose())
    base.EndRun()

module Program =
  [<EntryPoint>]
  let main args =
    use game = new TsuyoGame() in
      game.Run()
    0