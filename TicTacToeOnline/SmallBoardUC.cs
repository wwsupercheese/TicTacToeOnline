using System;
using System.Drawing;
using System.Windows.Forms;

namespace TicTacToeOnline
{
    public class SmallBoardUC : UserControl
    {
        public Button[] Buttons = new Button[9];
        public event Action<int>? OnCellClick;
        public int BoardIdx { get; }

        public SmallBoardUC(int idx)
        {
            BoardIdx = idx;
            Size = new Size(150, 150);
            Margin = new Padding(5);
            BackColor = Color.White;
            
            for (int i = 0; i < 9; i++)
            {
                int localIdx = i;
                Buttons[i] = new Button
                {
                    Size = new Size(50, 50),
                    Location = new Point((i % 3) * 50, (i / 3) * 50),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Arial", 16, FontStyle.Bold),
                    BackColor = Color.White,
                    TabStop = false
                };
                Buttons[i].FlatAppearance.BorderColor = Color.Black;
                Buttons[i].Click += (s, e) => OnCellClick?.Invoke(localIdx);
                Controls.Add(Buttons[i]);
            }
        }

        public void UpdateBoard(string subBoard, char winner, bool isMyTurn, bool isBoardActive)
        {
            for (int i = 0; i < 9; i++)
            {
                char cell = subBoard[i];
                Buttons[i].Text = cell == '.' ? "" : cell.ToString();
                
                if (cell == 'X') Buttons[i].ForeColor = Color.Blue;
                else if (cell == 'O') Buttons[i].ForeColor = Color.Red;
            }

            if (winner != '.')
            {
                Color winColor = winner == 'X' ? Color.LightBlue : Color.LightPink;
                this.BackColor = winColor;
                foreach (var btn in Buttons)
                {
                    btn.Enabled = false;
                    btn.BackColor = winColor;
                }
                return;
            }

            for (int i = 0; i < 9; i++)
            {
                char cell = subBoard[i];
                // ВАЖНО: Кнопки активируются только если это ход игрока, доска активна и ячейка пуста.
                // В режиме "Поиск сервера" внешняя логика MainForm принудительно отключит эти кнопки.
                bool canClick = (cell == '.') && isMyTurn && isBoardActive;
                Buttons[i].Enabled = canClick;

                if (canClick)
                    Buttons[i].BackColor = Color.White;
                else
                    Buttons[i].BackColor = cell == '.' ? Color.LightGray : Color.White;
            }
            
            this.BackColor = Color.White;
        }

        public void SetHighlight(bool active)
        {
            if (active)
            {
                this.BorderStyle = BorderStyle.Fixed3D;
                this.BackColor = Color.LightYellow;
            }
            else
            {
                this.BorderStyle = BorderStyle.None;
                if (BackColor == Color.LightYellow) BackColor = Color.Transparent;
            }
        }
    }
}