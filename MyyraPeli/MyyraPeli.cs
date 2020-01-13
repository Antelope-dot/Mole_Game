using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

///@author Ilari Massa
///@version 03.12.2018
/// <summary>
/// Maanalainen seikkailu peli
/// </summary>
/// Huom! Pelissä ei ole käytetty taulukko, eikä silmukkaa ks: https://tim.jyu.fi/answers/kurssit/tie/ohj1/2018s/demot/demo11?answerNumber=46&task=taulukot&user=ilahilma
public class MyyraPeli : PhysicsGame
{
    private int KENTTANRO = 1;
    private const double NOPEUS = 200;
    private const double HYPPYNOPEUS = 400;
    private const double RUUDUN_KOKO = 40;
    private int PISTEET = 0;

    /// <summary>
    /// Highscore List
    /// </summary>
    private ScoreList topLista = new ScoreList(5, true, 1000);

    private PlatformCharacter player1;

    private IntMeter pisteLaskuri;

    /// <summary>
    /// kaikki kuvat joita pelissä käytetään
    /// </summary>
    static readonly Image playerKuva = LoadImage("myyra");
    static readonly Image Kaarme1Kuva = LoadImage("kaarme1");
    static readonly Image piikitKuva = LoadImage("piikit");
    static readonly Image oviKuva = LoadImage("door");
    static readonly Image LaatikkoKuva = LoadImage("Laatikko");
    static readonly Image taustaKuva = LoadImage("maa");
    static readonly Image tiiliKuva = LoadImage("tiili");

    
    /// <summary>
    /// Aloittaa pelin
    /// </summary>
    public override void Begin()
    {
        SeuraavaKentta();
        IsFullScreen = false;
        topLista = DataStorage.TryLoad<ScoreList>(topLista, "pisteet.xml");
        MediaPlayer.Play("taustamusiikki");
        MediaPlayer.IsRepeating = true;
        
    }
   
    
    /// <summary>
    /// Asettaa kameran seuraamaan pelaajaa, näppäimet ja Painovoiman
    /// </summary>
    private void AsetaOhjaimet()
    {
        Gravity = new Vector(0, -1000);
        Camera.Follow(player1);
        Camera.ZoomFactor = 1.2;
        Camera.StayInLevel = true;

        LisaaNappaimet();
    }
    
    
    /// <summary>
    /// Aloitaa uuden kentän, ja tuo pistelaskurin pisteet seuraavaan kenttään
    /// </summary>
    private void SeuraavaKentta()
    {
        if (pisteLaskuri != null)
            PISTEET = pisteLaskuri.Value;
        ClearAll();

        if (KENTTANRO > 3)
        {
            Exit();
        }
        else LuoKentta("kentta" + KENTTANRO);

        AsetaOhjaimet();
        LuoPistelaskuri();

    }
    
    
    /// <summary>
    /// Luo kentan käyttäen tekstitiedostoa
    /// </summary>
    /// <param name="kentta"> Peli taso </param>
    public void LuoKentta(string kentta)
    {
        TileMap ruudut = TileMap.FromLevelAsset("kentta" + KENTTANRO);
        ruudut.SetTileMethod('=', LisaaTaso, tiiliKuva, 40.0,40.0);
        ruudut.SetTileMethod('M', LisaaPlayer);
        ruudut.SetTileMethod('K', LisaaKaarme);
        ruudut.SetTileMethod('O', LisaaInteraktiivinen,"ovi", oviKuva, 40.0);
        ruudut.SetTileMethod('P', LisaaInteraktiivinen, "vihollinen", piikitKuva, 40.0);
        ruudut.SetTileMethod('-', LisaaTaso, tiiliKuva,40.0, 10.0);
        ruudut.SetTileMethod('l', LisaaInteraktiivinen, "laatikko",LaatikkoKuva, 30.0);
        ruudut.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
        Level.CreateBorders();
        Level.Background.Image = taustaKuva;
        Level.Background.TileToLevel();
    }


    /// <summary>
    /// Luo yhden tiilen
    /// </summary>
    /// <param name="paikka"> minne tiili "syntyy"</param>
    /// <param name="leveys"> tiilen leveys </param>
    /// <param name="korkeus"> tiilen korkeus </param>
    public void LisaaTaso(Vector paikka, double leveys, double korkeus, Image kuva, double lev, double kork) 
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(lev, kork);
        taso.Position = paikka;
        taso.Image = kuva;


        taso.CollisionIgnoreGroup = 1;

        Add(taso);
    }
    
  
    /// <summary>
    /// Luo pelattavan hahmon
    /// </summary>
    /// <param name="paikka"> minne hahmo syntyy </param>
    /// <param name="leveys"> hahmon leveys </param>
    /// <param name="korkeus"> hahmon korkeus </param>
    public void LisaaPlayer(Vector paikka, double leveys, double korkeus)
    {
        player1 = new PlatformCharacter(korkeus * 0.75, leveys*0.75);
        player1.Position = paikka;
        player1.Mass = 600;
        player1.Image = playerKuva;
        AddCollisionHandler(player1, "vihollinen", TormaaVihu);
        AddCollisionHandler(player1, "ovi", TormaaOveen);
        Add(player1);
    }
    
    
    /// <summary>
    /// Lisaa Vihollisen
    /// </summary>
    /// <param name="paikka">minne kaarme syntyy</param>
    /// <param name="leveys">kaarmeen leveys</param>
    /// <param name="korkeus">kaarmeen korkeus</param>
    public void LisaaKaarme(Vector paikka, double leveys, double korkeus)
    {
        PlatformCharacter Kaarme1 = new PlatformCharacter(leveys * 0.80, korkeus * 0.80);
        Kaarme1.Position = paikka;
        Kaarme1.Mass = 4.0;
        Kaarme1.Tag = "vihollinen";
        Add(Kaarme1);

        PlatformWandererBrain tasoAivot = new PlatformWandererBrain();
        tasoAivot.Speed = 100;

        Kaarme1.Image = Kaarme1Kuva;
        Kaarme1.Brain = tasoAivot;
    }

    
    /// <summary>
    /// Lisaa Oven joka toimii kentan lopetuksena, piikin joka on vihollinen ja laatikon jota voi liikuttaa
    /// </summary>
    /// <param name="paikka">Minne ovi syntyy</param>
    /// <param name="leveys">oven leveys</param>
    /// <param name="korkeus">oven korkeus</param>
    public void LisaaInteraktiivinen(Vector paikka,double leveys, double korkeus, string tagi, Image kuva, double lev)
    {
        double k = 30;
        PhysicsObject Interaktiivinen = new PhysicsObject(lev, k);
        Interaktiivinen.Position = paikka;
        Interaktiivinen.Mass = 500;
        Interaktiivinen.Tag = tagi;
        Add(Interaktiivinen);
        
        Interaktiivinen.Image = kuva;
    }
    
   
    /// <summary>
    /// Luo pistelaskurin vasempaan yläkulmaan joka lisää yhden pisteen sekunnissa
    /// </summary>
    private void LuoPistelaskuri()
    {
        pisteLaskuri = new IntMeter(0);
          
        Label pisteNaytto = new Label();
        pisteNaytto.X = Screen.Left + 100;
        pisteNaytto.Y = Screen.Top - 100;
        pisteNaytto.TextColor = Color.Red;
        pisteNaytto.Color = Color.Transparent;
        pisteLaskuri.AddOverTime(1000, 1000);


        pisteNaytto.BindTo(pisteLaskuri);
        pisteLaskuri.Value = PISTEET;
        Add(pisteNaytto);
    }
   
    
    /// <summary>
    /// Luo näppäimet joilla hahmoa voi liikuttaa, ja lopettaa pelin
    /// </summary>
    private void LisaaNappaimet()
    {
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Left, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", player1, -NOPEUS);
        Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, "Liikkuu oikealle", player1, NOPEUS);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", player1, HYPPYNOPEUS);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }
    
    
    /// <summary>
    /// Liikkeen nopeus kun painaa näppäintä
    /// </summary>
    /// <param name="hahmo">pelaaja hahmo</param>
    /// <param name="nopeus">liikkuttava nopeus kun npainaa nuolinäppäintä</param>
    public void Liikuta(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Walk(nopeus);
    }
    
    
    /// <summary>
    /// Hypyn nopeus
    /// </summary>
    /// <param name="hahmo">pelaaja hahmo</param>
    /// <param name="nopeus">liikuttava nopeus ylöspäin</param>
    public void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Jump(nopeus);
    }
    
    
    /// <summary>
    /// Pelaajan törmätessä viholliseen, kenttä alkaa alusta ja soittaa Wilhelm Huudon
    /// </summary>
    /// <param name="hahmo">Pelaaja Hahmo</param>
    /// <param name="vihollinen">Vihollinen</param>
    public void TormaaVihu(PhysicsObject hahmo, PhysicsObject vihollinen)
    {
        SoundEffect kuolema = LoadSoundEffect("kuolema");   // AJ:N ehdotus
        kuolema.Play();
        SeuraavaKentta();
    }
    
   
    /// <summary>
    /// Prosessoi mitä vastaus laatikkoon vastattiin
    /// </summary>
    /// <param name="ikkuna">Vastaus laatikon syötetty vastaus</param>
    private void ProcessInput(InputWindow ikkuna)
    {
        string vastaus = ikkuna.InputBox.Text;

        if(KENTTANRO == 1 || KENTTANRO == 2)
        {
            if (vastaus.Contains("3") || vastaus.Contains("kyllä") || vastaus.Contains("Kyllä") || vastaus.Contains("madot") ||
            vastaus.Contains("Madot"))
            {
                KENTTANRO++;
                SeuraavaKentta();
            }

            else
            {
                pisteLaskuri.Value += 10;
            }
        }

        if(KENTTANRO == 3)
        {
            if (vastaus.Contains("Arvicolinae") || vastaus.Contains("arvicolinae") || vastaus.Contains("1"))
            {
                
                HighScoreWindow topIkkuna = new HighScoreWindow(
                             "Pääsit pakoon!",
                             "Onneksi olkoon, pääsit listalle pisteillä %p! Syötä nimesi:",
                             topLista, pisteLaskuri.Value);
                topIkkuna.Closed += TallennaPisteet;
                Add(topIkkuna);
            }


            else
            {
                pisteLaskuri.Value += 10;
            }
        }


    }


    /// <summary>
    /// Esittää pelaajalle kysymyksen hänen törmätessä maaliin
    /// </summary>
    /// <param name="pelaaja">pela</param>
    /// <param name="maali"></param>
    public void TormaaOveen(PhysicsObject pelaaja, PhysicsObject maali)
    {
        if (KENTTANRO == 1)
        {
            InputWindow kysymysIkkuna = new InputWindow("Mikä on myyrän lempi ruoka? 1. Pizza , 2. Pasta, 3. Madot");
            kysymysIkkuna.TextEntered += ProcessInput;
            Add(kysymysIkkuna);
        }

        if (KENTTANRO == 2)
        {
            InputWindow kysymysIkkuna = new InputWindow("Onko piisami myyrä?(vastaa kyllä tai ei)");
            kysymysIkkuna.TextEntered += ProcessInput;
            Add(kysymysIkkuna);
        }

        if (KENTTANRO == 3)
        {
            InputWindow kysymysIkkuna = new InputWindow("Miksi myyrien alaheimoa kutsutaan? 1.Arvicolinae, 2.Cricetidae, 3.Sigmodontinae ");
            kysymysIkkuna.TextEntered += ProcessInput;
            Add(kysymysIkkuna);
        }
    }


    /// <summary>
    /// tallentaa pisteet ja sulkee pelin
    /// </summary>
    /// <param name="sender">mitä tapahtuu kun Score List laitetaan kiinni</param>
    public void TallennaPisteet(Window sender)
    {
        DataStorage.Save<ScoreList>(topLista, "pisteet.xml");
        KENTTANRO++;
        SeuraavaKentta();
    }
}

