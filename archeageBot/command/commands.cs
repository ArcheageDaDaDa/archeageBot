using Discord;
using Discord.Commands;
using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArcheageBot.command
{
    public class commands : ModuleBase<SocketCommandContext>
    {
        private static readonly HttpClient HTTP_CLIENT = new HttpClient();
        private const string ARCHEAGE_LINK = "https://archeage.xlgames.com/";
        private static readonly HttpClient httpClient = new HttpClient();
        [Command("ping")]
        public async Task PingAsync()
        {
            var sw = Stopwatch.StartNew();
            var msg = await ReplyAsync($"**Websocket latency**: {Context.Client.Latency}ms\n" +
                                        "**Response**: ...");
            sw.Stop();
            await msg.ModifyAsync(x => x.Content = $"**Websocket latency**: {Context.Client.Latency}ms\n" +
                                                   $"**Response**: {(int)sw.Elapsed.TotalMilliseconds}ms");
        }

        [Command("아키")]
        public async Task ArcheageSearchAsync(string server, string name)
        {
            Regex pattanString = new Regex("^[a-zA-Z]*$", RegexOptions.Compiled);
            Match strings = pattanString.Match(name);

            string str = name;

            try
            {
                string CharactorName = "";
                if (strings.Success)
                {
                    CharactorName = char.ToUpper(str[0]) + str.Substring(1);
                }
                else
                {
                    CharactorName = name;
                }

                string serverString = null;
                if (server == "하제")
                {
                    serverString = "HAJE";
                }
                else if (server == "누이")
                {
                    serverString = "NUI";
                }
                else if (server == "오키드나")
                {
                    serverString = "ORCHIDNA";
                }
                else if (server == "다미안")
                {
                    serverString = "DAMIAN";
                }
                else if (server == "에안나")
                {
                    serverString = "EANNA";
                }
                else if (server == "정원")
                {
                    serverString = "GARDEN";
                }
                else if (server == "정원2")
                {
                    serverString = "GARDEN2";
                }
                else if (server == "다후타")
                {
                    serverString = "DAHUTA";
                }
                else if (server == "모르페우스" || server == "몰페")
                {
                    serverString = "MORPHEUS";
                    server = "모르페우스";
                }
                else
                {
                    serverString = "HAJE";
                    server = "하제";
                }

                using (var response = await httpClient.GetAsync($"{ARCHEAGE_LINK}search?dt=characters&keyword={name}&server={serverString}").ConfigureAwait(false))
                {
                    string contentDetailsString = await response.Content.ReadAsStringAsync();

                    HtmlDocument pageDocument = new HtmlDocument();
                    pageDocument.LoadHtml(contentDetailsString);

                    HtmlNodeCollection headlineArray = pageDocument.DocumentNode.SelectNodes("//span[@class='character_card']");

                    string message = "";
                    string userUrl = "";
                    string characterName = "";
                    string[] messageName = new string[10];
                    foreach (HtmlNode node in headlineArray)
                    {
                        var nodeData = node.ChildNodes[1];
                        string href = nodeData.GetAttributeValue("href", null);

                        characterName = node.InnerText.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");

                        if (characterName == name + $"@{server}")
                        {
                            message = characterName + "님의 케릭터 링크는 " + href;
                            userUrl = href;
                            break;
                        }

                    }
                    if (message == "")
                    {
                        message = "케릭터를 찾지 못했습니다.";
                        await ReplyAsync(message);
                    }
                    else
                    {
                        using (var detailUserResponse = await httpClient.GetAsync(userUrl).ConfigureAwait(false))
                        {
                            string userContentDetailsString = await detailUserResponse.Content.ReadAsStringAsync();

                            HtmlDocument userPageDocument = new HtmlDocument();
                            userPageDocument.LoadHtml(userContentDetailsString);

                            // HtmlNodeCollection mainBrief = pageDocument.DocumentNode.SelectNodes(".//div[@class='m_news']/ul[1]/li/a");

                            string userHP = userPageDocument.DocumentNode.SelectSingleNode(".//div[@class='left']/dl[1]/dd").InnerText;
                            string userMP = userPageDocument.DocumentNode.SelectSingleNode(".//div[@class='left']/dl[2]/dd").InnerText;
                            // 장비 점수
                            string userEquipmentScore = userPageDocument.DocumentNode.SelectSingleNode(".//div[@class='bor']/dl/dd").InnerText;
                            string profileImgUrl = userPageDocument.DocumentNode.SelectSingleNode(".//div[@class='character_card']/a[@class='character_name']/img[@class='character_thumb']").Attributes["src"].Value;
                            // 유연성
                            string userFlexibility = userPageDocument.DocumentNode.SelectSingleNode(".//div[contains(@class, 'characterStatTabContent_3')]/dl[4]/dd").InnerText;
                            // 머리 전숙
                            string userHeadCombat = userPageDocument.DocumentNode.SelectSingleNode(".//div[contains(@class, 'characterStatTabContent_3')]/dl[5]/dd").InnerText;

                            EmbedBuilder embedBuilder = new EmbedBuilder()
                            {
                                Title = $"아키에이지 {characterName}님의 케릭터 정보입니다"
                            };

                            // 원정대
                            var userExpedition = userPageDocument.DocumentNode.SelectSingleNode("//span[@class='character_exped']/a");
                            string userExpeditionUrl = "";
                            string userExpeditionName = "";
                            if (userExpedition != null)
                            {
                                // 원정대 링크
                                userExpeditionUrl = userExpedition.Attributes["href"].Value;
                                // 원정대 이름
                                userExpeditionName = userPageDocument.DocumentNode.SelectSingleNode("//span[@class='character_exped']/a/span").InnerText;

                                embedBuilder.AddField("원정대", userExpeditionName, false);
                                embedBuilder.AddField("원정대링크", $"https://archeage.xlgames.com{userExpeditionUrl}", false);
                            }
                            else
                            {
                                userExpeditionName = "원정대 없음";
                                embedBuilder.AddField("원정대", userExpeditionName, false);
                            }

                            embedBuilder.AddField("장비점수", userEquipmentScore, true);
                            embedBuilder.AddField("생명력", userHP, true);
                            embedBuilder.AddField("활력", userMP, true);
                            embedBuilder.AddField("유연성", userFlexibility, true);
                            embedBuilder.AddField("전투숙련", userHeadCombat, true);
                            embedBuilder.AddField("케릭터 링크", userUrl, false);
                            embedBuilder.WithThumbnailUrl(profileImgUrl);
                            embedBuilder.WithColor(new Color(0, 206, 56));
                            embedBuilder.WithFooter("powerd by archeage.xlgames.com");
                            await ReplyAsync(string.Empty, false, embedBuilder.Build()).ConfigureAwait(false);
                        }

                        //await command.ReplySuccess(Communicator, message);
                    }

                }
            }
            catch (Exception e)
            {
                Console.Write($"캐릭터를 찾지 못했습니다. {e.Message}");
                await ReplyAsync($"캐릭터를 찾지 못했습니다. {e.Message}");
            }
        }
    }
}
