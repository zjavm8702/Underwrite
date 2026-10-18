using System;
using System.Collections.Generic;
using Underwrite.Global;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 미니게임 규칙서의 쪽들(주사위 · 주문서 · 기록 짜맞추기). 무대에는 실제 게임 화면과 같은 부품을 세우고
    /// schedule로 순서대로 움직인다 — 한 번 보고 끝나지 않게 몇 초마다 처음부터 되풀이한다.
    /// </summary>
    public static class MiniGameRules
    {
        private const long STEP_MS = 900;

        // ── 주사위 내기 ─────────────────────────

        public static List<RulePage> Dice()
        {
            return new List<RulePage>
            {
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_DICE_1_TITLE,
                    Body = StringConstants.TEXT_RULE_DICE_1_BODY,
                    Stage = set =>
                    {
                        var opponent = DiceRow(set, StringConstants.TEXT_RULE_OPPONENT, new[] { 0, 0, 0, 0 }, -1, false);
                        var mine = DiceRow(set, StringConstants.TEXT_RULE_ME, new[] { 4, 4, 2, 6 }, -1, false);
                        Loop(set, 4, step =>
                        {
                            // 굴리는 동안 눈이 바뀌다 멈춘다.
                            var faces = step < 3 ? new[] { (step * 2 + 1) % 6 + 1, (step + 4) % 6 + 1, (step * 3) % 6 + 1, (step + 2) % 6 + 1 } : new[] { 4, 4, 2, 6 };
                            SetDice(mine, faces, -1, false);
                            opponent.EnableInClassList("rule-row--rolling", step < 3);
                        });
                    }
                },
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_DICE_2_TITLE,
                    Body = StringConstants.TEXT_RULE_DICE_2_BODY,
                    Stage = set =>
                    {
                        Bubble(set, StringConstants.TEXT_RULE_DICE_2_BUBBLE);
                        var opponent = DiceRow(set, StringConstants.TEXT_RULE_OPPONENT, new[] { 1, 4, 5, 3 }, -1, true);
                        var mine = DiceRow(set, StringConstants.TEXT_RULE_ME, new[] { 4, 4, 2, 6 }, -1, false);
                        var counter = Caption(set, string.Empty);
                        var order = new List<Label>();
                        Collect(mine, 4, order);
                        Collect(opponent, 4, order);

                        Loop(set, order.Count + 2, step =>
                        {
                            for (var i = 0; i < order.Count; i++)
                            {
                                order[i].EnableInClassList("dice-die--match", i < step);
                            }

                            var counted = Math.Min(step, order.Count);
                            counter.text = counted == 0 ? string.Empty : string.Format(StringConstants.TEXT_RULE_DICE_2_COUNT, counted);
                        });
                    }
                },
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_DICE_3_TITLE,
                    Body = StringConstants.TEXT_RULE_DICE_3_BODY,
                    Stage = set =>
                    {
                        var ladder = new List<Label>();

                        foreach (var line in StringConstants.TEXT_RULE_DICE_3_LADDER)
                        {
                            var bubble = Bubble(set, line);
                            bubble.EnableInClassList("rule-bubble--no", line.StartsWith(StringConstants.TEXT_RULE_MARK_NO) == true);
                            ladder.Add(bubble);
                        }

                        Loop(set, ladder.Count + 2, step =>
                        {
                            for (var i = 0; i < ladder.Count; i++)
                            {
                                ladder[i].EnableInClassList("rule-hidden", i >= step);
                            }
                        });
                    }
                },
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_DICE_4_TITLE,
                    Body = StringConstants.TEXT_RULE_DICE_4_BODY,
                    Stage = set =>
                    {
                        Bubble(set, StringConstants.TEXT_RULE_DICE_4_BUBBLE);
                        // 말풍선을 가리지 않게 도장은 그 아래 줄에 찍는다.
                        var stamp = Stamp(set, StringConstants.TEXT_RULE_DICE_4_STAMP);
                        stamp.AddToClassList("rule-stamp--inline");
                        var opponent = DiceRow(set, StringConstants.TEXT_RULE_OPPONENT, new[] { 6, 2, 1, 3 }, -1, false);
                        var mine = DiceRow(set, StringConstants.TEXT_RULE_ME, new[] { 6, 4, 4, 5 }, -1, false);
                        var result = Caption(set, StringConstants.TEXT_RULE_DICE_4_RESULT);

                        Loop(set, 6, step =>
                        {
                            stamp.EnableInClassList("rule-hidden", step < 1);
                            var open = step >= 2;
                            SetDice(opponent, new[] { 6, 2, 1, 3 }, open == true ? 6 : -1, open == false);
                            SetDice(mine, new[] { 6, 4, 4, 5 }, open == true ? 6 : -1, false);
                            result.EnableInClassList("rule-hidden", step < 3);
                        });
                    }
                },
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_DICE_5_TITLE,
                    Body = StringConstants.TEXT_RULE_DICE_5_BODY,
                    Stage = set =>
                    {
                        var row = Row(set);
                        var tokens = new List<Label>();

                        foreach (var round in StringConstants.TEXT_RULE_DICE_5_ROUNDS)
                        {
                            var token = Token(row, round);
                            tokens.Add(token);
                        }

                        Loop(set, 4, step =>
                        {
                            for (var i = 0; i < tokens.Count; i++)
                            {
                                var won = i < 2 && i < step;
                                tokens[i].text = won == true
                                    ? StringConstants.TEXT_RULE_DICE_5_ROUNDS[i] + " " + StringConstants.TEXT_RULE_WIN
                                    : StringConstants.TEXT_RULE_DICE_5_ROUNDS[i];
                                tokens[i].EnableInClassList("rule-token--won", won);
                                tokens[i].EnableInClassList("rule-token--unused", i == 2 && step >= 2);
                            }
                        });
                    }
                }
            };
        }

        // ── 주문서 읽기 ─────────────────────────

        public static List<RulePage> Smith()
        {
            var name = StringConstants.TEXT_SMITH_TUT_TARGET;
            var items = StringConstants.TEXT_SMITH_TUT_ITEMS;
            var status = StringConstants.TEXT_SMITH_STATUS;

            return new List<RulePage>
            {
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_SMITH_1_TITLE,
                    Body = StringConstants.TEXT_RULE_SMITH_1_BODY,
                    Stage = set =>
                    {
                        var slip = Slip(set, string.Format(StringConstants.TEXT_SMITH_DAY, 2), status[2], name, items[3], true);
                        var callouts = Row(set);
                        var labels = new List<Label>();

                        foreach (var text in StringConstants.TEXT_RULE_SMITH_1_CALLOUTS)
                        {
                            labels.Add(Token(callouts, text));
                        }

                        Loop(set, labels.Count + 1, step =>
                        {
                            for (var i = 0; i < labels.Count; i++)
                            {
                                labels[i].EnableInClassList("rule-token--won", i == step);
                            }

                            // 짚는 칸을 주문서에서도 밝힌다(날짜 · 상태 · 단 · 물건 순).
                            for (var i = 0; i < slip.Count; i++)
                            {
                                slip[i].EnableInClassList("rule-glow", i == step);
                            }
                        });
                    }
                },
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_SMITH_2_TITLE,
                    Body = StringConstants.TEXT_RULE_SMITH_2_BODY,
                    Stage = set =>
                    {
                        var row = Row(set);
                        var old = SlipCard(row, StringConstants.TEXT_SMITH_LAST_SEASON, status[0], name, items[0]);
                        SlipCard(row, string.Format(StringConstants.TEXT_SMITH_DAY, 2), status[2], name, items[3]);
                        Loop(set, 3, step => old.EnableInClassList("rule-struck", step >= 1));
                    }
                },
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_SMITH_3_TITLE,
                    Body = StringConstants.TEXT_RULE_SMITH_3_BODY,
                    Stage = set =>
                    {
                        var row = Row(set);
                        var cancelled = SlipCard(row, string.Format(StringConstants.TEXT_SMITH_DAY, 1), status[3], name, items[2]);
                        SlipCard(row, string.Format(StringConstants.TEXT_SMITH_DAY, 2), status[2], name, items[3]);
                        var stamp = Stamp(cancelled, status[3]);
                        Loop(set, 3, step =>
                        {
                            stamp.EnableInClassList("rule-hidden", step < 1);
                            cancelled.EnableInClassList("rule-struck", step >= 2);
                        });
                    }
                },
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_SMITH_4_TITLE,
                    Body = StringConstants.TEXT_RULE_SMITH_4_BODY,
                    Stage = set =>
                    {
                        var row = Row(set);
                        var old = SlipCard(row, StringConstants.TEXT_SMITH_LAST_SEASON, status[0], name, items[0]);
                        var cancelled = SlipCard(row, string.Format(StringConstants.TEXT_SMITH_DAY, 1), status[3], name, items[2]);
                        var proof = SlipCard(row, string.Format(StringConstants.TEXT_SMITH_DAY, 2), status[2], name, items[3]);
                        Loop(set, 4, step =>
                        {
                            old.EnableInClassList("rule-struck", step >= 1);
                            cancelled.EnableInClassList("rule-struck", step >= 2);
                            proof.EnableInClassList("rule-glow", step >= 3);
                        });
                    }
                }
            };
        }

        // ── 기록 짜맞추기 ───────────────────────

        public static List<RulePage> Deduce()
        {
            return new List<RulePage>
            {
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_DEDUCE_1_TITLE,
                    Body = StringConstants.TEXT_RULE_DEDUCE_1_BODY,
                    Stage = set =>
                    {
                        var clues = new List<Label>();

                        foreach (var text in StringConstants.TEXT_RULE_DEDUCE_1_CLUES)
                        {
                            var clue = new Label(text);
                            clue.AddToClassList("popup");
                            clue.AddToClassList("deduce-clue");
                            clue.AddToClassList("rule-clue");
                            set.Add(clue);
                            clues.Add(clue);
                        }

                        Loop(set, clues.Count + 2, step =>
                        {
                            for (var i = 0; i < clues.Count; i++)
                            {
                                clues[i].EnableInClassList("rule-hidden", i >= step);
                            }
                        });
                    }
                },
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_DEDUCE_2_TITLE,
                    Body = StringConstants.TEXT_RULE_DEDUCE_2_BODY,
                    Stage = set =>
                    {
                        var row = Row(set);
                        var band = new Label(StringConstants.TEXT_LEDGER_FILLER_BANDS[0]);
                        band.AddToClassList("deduce-band");
                        row.Add(band);
                        var cell = new Label();
                        cell.AddToClassList("deduce-pick");
                        cell.AddToClassList("rule-cell");
                        row.Add(cell);
                        var options = StringConstants.TEXT_RULE_DEDUCE_3_COLS;

                        Loop(set, options.Length + 1, step =>
                        {
                            // 빈칸 → 보기 1 → 보기 2 → 보기 3 → 빈칸. 누르는 것처럼 칸이 한 번 눌린다.
                            cell.text = step == 0 ? StringConstants.TEXT_DEDUCE_BLANK : options[step - 1];
                            cell.EnableInClassList("deduce-pick--blank", step == 0);
                            cell.EnableInClassList("rule-pressed", true);
                            cell.schedule.Execute(() => cell.EnableInClassList("rule-pressed", false)).StartingIn(150);
                        });
                    }
                },
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_DEDUCE_3_TITLE,
                    Body = StringConstants.TEXT_RULE_DEDUCE_3_BODY,
                    Stage = set =>
                    {
                        var caption = Caption(set, string.Empty);
                        var grid = Grid(set, StringConstants.TEXT_RULE_DEDUCE_3_ROWS, StringConstants.TEXT_RULE_DEDUCE_3_COLS);
                        var steps = StringConstants.TEXT_RULE_DEDUCE_3_STEPS;

                        Loop(set, steps.Length + 2, step =>
                        {
                            caption.text = step == 0 ? string.Empty : steps[Math.Min(step, steps.Length) - 1];
                            Mark(grid[0][1], step >= 1, false);
                            Mark(grid[0][2], step >= 2, false);
                            Mark(grid[0][0], step >= 3, true);
                            Mark(grid[1][0], step >= 4, false);
                            Mark(grid[2][0], step >= 4, false);
                        });
                    }
                },
                new RulePage
                {
                    Title = StringConstants.TEXT_RULE_DEDUCE_4_TITLE,
                    Body = StringConstants.TEXT_RULE_DEDUCE_4_BODY,
                    Stage = set =>
                    {
                        var row = Row(set);
                        var tokens = new List<Label>();

                        for (var i = 0; i < Constants.DEDUCE_TRIES; i++)
                        {
                            tokens.Add(Token(row, StringConstants.TEXT_RULE_MARK_YES));
                        }

                        Loop(set, tokens.Count + 2, step =>
                        {
                            for (var i = 0; i < tokens.Count; i++)
                            {
                                tokens[i].EnableInClassList("rule-token--unused", i >= tokens.Count - step + 1 && step <= tokens.Count);
                            }
                        });
                    }
                }
            };
        }

        // ── 무대 부품 ───────────────────────────

        // steps 칸을 STEP_MS마다 0, 1, …, steps-1 순으로 되풀이한다. 무대 요소가 사라지면 함께 멈춘다.
        private static void Loop(VisualElement set, int steps, Action<int> apply)
        {
            var step = 0;
            apply(0);
            set.schedule.Execute(() =>
            {
                step = (step + 1) % steps;
                apply(step);
            }).Every(STEP_MS).StartingIn(STEP_MS);
        }

        private static VisualElement Row(VisualElement set)
        {
            var row = new VisualElement();
            row.AddToClassList("rule-row");
            set.Add(row);
            return row;
        }

        private static VisualElement DiceRow(VisualElement set, string who, int[] faces, int match, bool ghost)
        {
            var row = Row(set);
            var label = new Label(who);
            label.AddToClassList("rule-who");
            row.Add(label);

            for (var i = 0; i < faces.Length; i++)
            {
                var die = new Label();
                die.AddToClassList("dice-die");
                die.AddToClassList("rule-die");
                die.EnableInClassList("rule-die--ghost", ghost);
                row.Add(die);
            }

            SetDice(row, faces, match, faces[0] == 0);
            return row;
        }

        // 한 줄의 주사위 눈을 바꾼다. 0이거나 hidden이면 「?」. match와 같은 눈은 밝힌다.
        private static void SetDice(VisualElement row, int[] faces, int match, bool hidden)
        {
            var index = 0;

            foreach (var child in row.Children())
            {
                var die = child as Label;

                if (die == null || die.ClassListContains("dice-die") == false)
                {
                    continue;
                }

                var face = faces[index++];
                var cover = hidden == true || face == 0;
                die.text = cover == true ? StringConstants.TEXT_DICE_HIDDEN : face.ToString();
                die.EnableInClassList("dice-die--hidden", cover);
                die.EnableInClassList("dice-die--match", cover == false && face == match);
            }
        }

        private static void Collect(VisualElement row, int face, List<Label> into)
        {
            foreach (var child in row.Children())
            {
                var die = child as Label;

                if (die != null && die.ClassListContains("dice-die") == true && die.text == face.ToString())
                {
                    into.Add(die);
                }
            }
        }

        private static Label Bubble(VisualElement set, string text)
        {
            var bubble = new Label(text);
            bubble.AddToClassList("rule-bubble");
            set.Add(bubble);
            return bubble;
        }

        private static Label Caption(VisualElement set, string text)
        {
            var caption = new Label(text);
            caption.AddToClassList("rule-caption");
            set.Add(caption);
            return caption;
        }

        private static Label Stamp(VisualElement parent, string text)
        {
            var stamp = new Label(text);
            stamp.AddToClassList("rule-stamp");
            stamp.pickingMode = PickingMode.Ignore;
            parent.Add(stamp);
            return stamp;
        }

        private static Label Token(VisualElement row, string text)
        {
            var token = new Label(text);
            token.AddToClassList("rule-token");
            row.Add(token);
            return token;
        }

        // 큰 주문서 한 장. 짚을 칸(날짜 · 상태 · 단 · 물건) 라벨을 순서대로 돌려준다.
        private static List<Label> Slip(VisualElement set, string day, string status, string band, string item, bool big)
        {
            var card = SlipCard(set, day, status, band, item);
            card.EnableInClassList("rule-slip--big", big);
            var parts = new List<Label>();

            foreach (var child in card.Query<Label>().ToList())
            {
                if (child.ClassListContains("rule-stamp") == false)
                {
                    parts.Add(child);
                }
            }

            return parts;
        }

        private static VisualElement SlipCard(VisualElement parent, string day, string status, string band, string item)
        {
            var card = new VisualElement();
            card.AddToClassList("popup");
            card.AddToClassList("smith-slip");
            card.AddToClassList("rule-slip");

            var top = new VisualElement();
            top.AddToClassList("smith-slip-top");
            var dayLabel = new Label(day);
            dayLabel.AddToClassList("smith-slip-day");
            var statusLabel = new Label(status);
            statusLabel.AddToClassList("smith-slip-status");
            statusLabel.EnableInClassList("smith-slip-status--cancelled", status == StringConstants.TEXT_SMITH_STATUS[StringConstants.TEXT_SMITH_STATUS.Length - 1]);
            top.Add(dayLabel);
            top.Add(statusLabel);

            var bandLabel = new Label(band);
            bandLabel.AddToClassList("smith-slip-band");
            var itemLabel = new Label(item);
            itemLabel.AddToClassList("smith-slip-item");

            card.Add(top);
            card.Add(bandLabel);
            card.Add(itemLabel);
            parent.Add(card);
            return card;
        }

        // 지워 가기 시연용 표. [행][열] 칸 라벨을 돌려준다.
        private static List<List<Label>> Grid(VisualElement set, string[] rows, string[] cols)
        {
            var grid = new List<List<Label>>();
            var head = Row(set);
            var corner = new Label(string.Empty);
            corner.AddToClassList("rule-grid-head");
            head.Add(corner);

            foreach (var col in cols)
            {
                var label = new Label(col);
                label.AddToClassList("rule-grid-head");
                head.Add(label);
            }

            foreach (var rowName in rows)
            {
                var row = Row(set);
                var name = new Label(rowName);
                name.AddToClassList("rule-grid-head");
                row.Add(name);
                var cells = new List<Label>();

                for (var i = 0; i < cols.Length; i++)
                {
                    var cell = new Label(string.Empty);
                    cell.AddToClassList("rule-grid-cell");
                    row.Add(cell);
                    cells.Add(cell);
                }

                grid.Add(cells);
            }

            return grid;
        }

        private static void Mark(Label cell, bool on, bool yes)
        {
            if (on == false)
            {
                cell.text = string.Empty;
                cell.RemoveFromClassList("rule-grid-cell--no");
                cell.RemoveFromClassList("rule-grid-cell--yes");
                return;
            }

            cell.text = yes == true ? StringConstants.TEXT_RULE_MARK_YES : StringConstants.TEXT_RULE_MARK_NO;
            cell.EnableInClassList("rule-grid-cell--yes", yes);
            cell.EnableInClassList("rule-grid-cell--no", yes == false);
        }
    }
}
