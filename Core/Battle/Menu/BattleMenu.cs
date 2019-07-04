﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenVIII
{
    /// <summary>
    /// Character BattleMenu
    /// </summary>
    public class BattleMenu : Menu
    {
        //private Mode _mode = Mode.Waiting;

        #region Constructors

        public BattleMenu(Characters character, Characters? visablecharacter = null) : base(character, visablecharacter)
        {
        }

        #endregion Constructors

        #region Enums

        public enum Mode : byte
        {
            Waiting,
            YourTurn,
            GF_Charging,
        }

        public enum SectionName : byte
        {
            Commands
        }

        #endregion Enums

        #region Methods

        /// <summary>
        /// <para>Draws the IGMData</para>
        /// <para>Skips Start and Stop because this class should be in another class</para>
        /// </summary>
        public override void Draw()
        {
            base.DrawData();
        }

        protected override void Init()
        {
            Size = new Vector2 { X = 840, Y = 630 };
            SetMode((Mode)0);
            Data.Add(SectionName.Commands, new IGMData_Commands(new Rectangle(50, (int)(Size.Y - 204), 210, 192)));
            base.Init();
        }

        protected override bool Inputs() => Data[SectionName.Commands].Inputs();

        #endregion Methods

    }

    /// <summary>
    /// Menu holds a menu for each character.
    /// </summary>
    public class BattleMenus : Menus
    {

        #region Fields

        private Dictionary<Mode, Action> DrawActions;
        private int _player = 0;
        private Dictionary<Mode, Func<bool>> UpdateFunctions;

        public int Player { get => _player; set => _player = value; }

        #endregion Fields

        //private Mode _mode = Mode.Starting;

        #region Enums

        public enum Mode : byte
        {
            Starting,
            Battle,
            Victory,
            GameOver,
        }

        #endregion Enums

        #region Methods

        public override void Draw()
        {
            StartDraw();
            DrawData();
            menus?.ForEach(m => m.Draw());
            EndDraw();
        }

        public override void DrawData()
        {
            DrawActions[(Mode)GetMode()]();
            base.DrawData();
        }

        public override void ReInit()
        {
            if (Memory.State?.Characters != null)
            {
                IEnumerable<KeyValuePair<int, Characters>> party = Memory.State.Party.Select((element, index) => new { element, index }).ToDictionary(m => m.index, m => m.element).Where(m => !m.Value.Equals(Characters.Blank));
                int count = party.Count();
                menus = new List<Menu>(count);
                foreach (KeyValuePair<int, Characters> m in party)
                {
                    BattleMenu tmp = new BattleMenu(Memory.State.PartyData[m.Key], m.Value);
                    tmp.Hide();
                    menus.Add(tmp);
                }
                SetMode(Mode.Battle);
                UpdateFunctions = new Dictionary<Mode, Func<bool>>()
                {
                    {Mode.Starting, UpdateStartingFunction},
                    {Mode.Battle, UpdateBattleFunction},
                    {Mode.Victory, UpdateVictoryFunction},
                    {Mode.GameOver, UpdateGameOverFunction},
                };
                DrawActions = new Dictionary<Mode, Action>()
                {
                    {Mode.Starting, DrawStartingAction},
                    {Mode.Battle, DrawBattleAction},
                    {Mode.Victory, DrawVictoryAction},
                    {Mode.GameOver, DrawGameOverAction},
                };
            }
            base.ReInit();
        }
        public override bool Update()
        {
            bool ret = UpdateFunctions[(Mode)GetMode()]();
            ret = base.Update() || ret;
            return ret;
        }

        private enum SectionName :byte
        {
            HP
        }
        protected override void Init()
        {
            Size = new Vector2 { X = 840, Y = 630 };
            SetMode((Mode)0);
            Data = new Dictionary<Enum, IGMData>()
            {
                {SectionName.HP, new IGMData_HP(new Rectangle((int)(Size.X-389),507,389,126))}
            };
            base.Init();
        }

        protected override bool Inputs() => false;

        private void DrawBattleAction() => menus[Player].Draw();

        private void DrawGameOverAction() => throw new NotImplementedException();

        private void DrawStartingAction() => throw new NotImplementedException();

        private void DrawVictoryAction() => throw new NotImplementedException();

        private bool UpdateBattleFunction()
        {
            menus[Player].Show();
            return menus[Player].Update();
        }

        private bool UpdateGameOverFunction() => throw new NotImplementedException();

        private bool UpdateStartingFunction() => throw new NotImplementedException();

        private bool UpdateVictoryFunction() => throw new NotImplementedException();

        private class IGMData_HP : IGMData
        {
            static Texture2D dot;
            public IGMData_HP(Rectangle pos) : base(3, 4, new IGMDataItem_Empty(pos), 1, 3)
            {
            }
            protected override void Init()
            {
                if (dot == null)
                {
                    dot = new Texture2D(Memory.graphics.GraphicsDevice, 1, 1);
                    lock(dot)
                    dot.SetData(new Color[] { Color.White });
                }
                base.Init();
            }

            public override void ReInit()
            {
                if (Memory.State?.Characters != null)
                {
                    IEnumerable<KeyValuePair<int, Characters>> party = Memory.State.Party.Select((element, index) => new { element, index }).ToDictionary(m => m.index, m => m.element).Where(m => !m.Value.Equals(Characters.Blank));
                    byte pos = 0;
                    foreach (var pm in party)
                    {
                        Saves.CharacterData c = Memory.State.Characters[Memory.State.PartyData[pm.Key]];
                        FF8String name = Memory.Strings.GetName(pm.Value);
                        int HP = c.CurrentHP(pm.Value);
                        //int MaxHP = c.MaxHP(pm.Value);
                        //float HPpercent = c.PercentFullHP(pm.Value);
                        int CriticalHP = c.CriticalHP(pm.Value);
                        Font.ColorID colorid = Font.ColorID.White;
                        byte palette = 2;
                        if (HP < CriticalHP)
                        {
                            colorid = Font.ColorID.Yellow;
                            palette = 6;
                        }
                        if (HP <= 0)
                        {
                            colorid = Font.ColorID.Red;
                            palette = 5;
                        }
                        byte? fadedpalette = null;
                        Font.ColorID? fadedcolorid = null;
                        if (BattleMenus.Player == pos)
                        {
                            fadedpalette = 7;
                            fadedcolorid = Font.ColorID.Grey;
                            ITEM[pos, 2] = new IGMDataItem_Texture(dot, new Rectangle(SIZE[pos].X + 230, SIZE[pos].Y + 12, 150, 15), Color.Yellow * .8f, Color.LightYellow);
                        }
                        // insert gradient atb bar here. Though this probably belongs in the update method as it'll be in constant flux.
                        else ITEM[pos, 2] = null;



                        // TODO: make a font render that can draw right to left from a point. For Right aligning the names.
                        ITEM[pos, 0] = new IGMDataItem_String(name, new Rectangle(SIZE[pos].X, SIZE[pos].Y, 0, 0), colorid,faded_color: fadedcolorid);
                        ITEM[pos, 1] = new IGMDataItem_Int(HP, new Rectangle(SIZE[pos].X + 128, SIZE[pos].Y, 0, 0), palette: palette, faded_palette: fadedpalette, spaces: 4, numtype: Icons.NumType.Num_8x16_1);
                        
                        ITEM[pos, 3] = new IGMDataItem_Icon(Icons.ID.Size_08x64_Bar, new Rectangle(SIZE[pos].X + 230, SIZE[pos].Y + 12, 150, 15),0);
                        pos++;
                    }
                    base.ReInit();
                }
            }
        }

        #endregion Methods

    }

    public abstract class Menus : Menu
    {

        #region Fields

        protected List<Menu> menus;

        #endregion Fields

    }
}
