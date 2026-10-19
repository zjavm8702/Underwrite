namespace Underwrite.Global
{
    /// <summary>
    /// 용사와 나누는 이야기 주제(2026-10-17, 용사 대화 늘리기). 주제 묶음은 <see cref="Patronage.HeroTopics"/>가 짠다.
    ///
    /// 언어 설정을 위해 배열을 쓰지 않고 한 줄에 상수 하나를 둔다. 표(UITextTable)가 열쇠로 덮어쓴다.
    /// 열쇠: TEXT_TOPIC_{주제}_OPEN(용사가 꺼내는 말), _REPLY_n(주인공의 대답), _ANSWER_n(그에 대한 용사의 말).
    /// </summary>
    public static partial class StringConstants
    {
        // ── 학자 ─────────────────────────────
        public static string TEXT_TOPIC_SCHOLAR_NOTES_OPEN = "(수첩에 무언가를 적다가) 아, 이거요? 의뢰마다 다 적어 둬요. 어디서 누가 다쳤는지, 뭘 놓쳤는지. …안 적으면 같은 실수를 하거든요.";
        public static string TEXT_TOPIC_SCHOLAR_NOTES_REPLY_1 = "꼼꼼하시네요. 다들 든든하겠어요.";
        public static string TEXT_TOPIC_SCHOLAR_NOTES_ANSWER_1 = "(안경을 밀어 올리며 조금 웃는다) …든든하다는 말은 처음 들어 봐요. 보통은 잔소리꾼이라고 하거든요.";
        public static string TEXT_TOPIC_SCHOLAR_NOTES_REPLY_2 = "저도 주문을 적어 둬야 안 잊어요.";
        public static string TEXT_TOPIC_SCHOLAR_NOTES_ANSWER_2 = "그렇죠? 적는 사람끼리는 통하는 게 있어요. (수첩 한 귀퉁이를 접는다)";
        public static string TEXT_TOPIC_SCHOLAR_LIBRARY_OPEN = "(창밖을 보며) 제가 원래 왕립 서고에서 일했던 거 아세요? 책으로 세상을 다 안다고 믿었죠. 그런데 책에는 사람이 어떻게 다치는지는 안 나와요.";
        public static string TEXT_TOPIC_SCHOLAR_LIBRARY_REPLY_1 = "그래서 서고를 나오신 거예요?";
        public static string TEXT_TOPIC_SCHOLAR_LIBRARY_ANSWER_1 = "네. 아는 것만으로는 아무도 못 지키더라고요. …이런 얘기, 일꾼 씨한테 처음 해요.";
        public static string TEXT_TOPIC_SCHOLAR_LIBRARY_REPLY_2 = "책이 더 안전하지 않아요?";
        public static string TEXT_TOPIC_SCHOLAR_LIBRARY_ANSWER_2 = "(쓴웃음) 안전하죠. 그래서 나왔어요. 안전한 자리에서 보기만 하는 건 싫어서.";
        public static string TEXT_TOPIC_SCHOLAR_RECKON_OPEN = "(목소리를 낮추며) 가끔 계산을 해 봐요. 우리가 이대로 가면 몇 년을 버틸 수 있을지. …답이 마음에 안 들어서 다시 계산하고요.";
        public static string TEXT_TOPIC_SCHOLAR_RECKON_REPLY_1 = "그 계산, 틀릴 거예요. 제가 장담해요.";
        public static string TEXT_TOPIC_SCHOLAR_RECKON_ANSWER_1 = "(한참 바라보다가) …일꾼 씨가 장담하면 왠지 믿고 싶어지네요. 이상하죠.";
        public static string TEXT_TOPIC_SCHOLAR_RECKON_REPLY_2 = "계산에 없는 게 있을지도 모르잖아요.";
        public static string TEXT_TOPIC_SCHOLAR_RECKON_ANSWER_2 = "계산에 없는 것… (수첩을 덮는다) 맞아요. 요즘 그런 게 하나 있긴 해요.";
        public static string TEXT_TOPIC_SCHOLAR_SUSPECT_OPEN = "(의뢰서 두 장을 펼친다) 일꾼 씨는 여관에 매일 계시잖아요. 이 의뢰를 낸 사람… 혹시 우리 테이블 근처에 자주 앉는 사람 없었어요?";
        public static string TEXT_TOPIC_SCHOLAR_SUSPECT_REPLY_1 = "글쎄요, 손님이 워낙 많아서요.";
        public static string TEXT_TOPIC_SCHOLAR_SUSPECT_ANSWER_1 = "그렇겠죠. (의뢰서를 접으며) 그래도 찾을 거예요. 고맙다는 말은 해야 하니까요.";
        public static string TEXT_TOPIC_SCHOLAR_SUSPECT_REPLY_2 = "찾아서 뭐 하시게요?";
        public static string TEXT_TOPIC_SCHOLAR_SUSPECT_ANSWER_2 = "모르겠어요. 고맙다고 해야 할지, 왜 우리를 아는지 물어야 할지.";

        // ── 검사 ─────────────────────────────
        public static string TEXT_TOPIC_SWORD_BLADE_OPEN = "(검을 손질하다 눈만 든다) 뭘 봐. 검 닦는 거 처음 봐?";
        public static string TEXT_TOPIC_SWORD_BLADE_REPLY_1 = "손이 다 갈라졌네요. 연고 갖다 드릴까요?";
        public static string TEXT_TOPIC_SWORD_BLADE_ANSWER_1 = "(손을 슬쩍 감추며) …필요 없어. (잠시 뒤) 어디 있는지만 알려 줘.";
        public static string TEXT_TOPIC_SWORD_BLADE_REPLY_2 = "멋있어서요.";
        public static string TEXT_TOPIC_SWORD_BLADE_ANSWER_2 = "(코웃음) 멋으로 드는 거 아니야. …그래도 칼날 보는 눈은 있네.";
        public static string TEXT_TOPIC_SWORD_VANGUARD_OPEN = "선봉이 뭔지 알아? 제일 먼저 들어가서 제일 늦게 나오는 자리야. 아무도 안 하려고 해서 내가 해.";
        public static string TEXT_TOPIC_SWORD_VANGUARD_REPLY_1 = "그럼 제일 먼저 돌아오셔야겠네요.";
        public static string TEXT_TOPIC_SWORD_VANGUARD_ANSWER_1 = "(멈칫) …그런 말 하는 사람은 처음이야. 노력은 해 볼게.";
        public static string TEXT_TOPIC_SWORD_VANGUARD_REPLY_2 = "무섭지 않아요?";
        public static string TEXT_TOPIC_SWORD_VANGUARD_ANSWER_2 = "무서워. 매번. 그래서 앞에 서는 거야. 뒤에서 보고만 있는 게 더 무서우니까.";
        public static string TEXT_TOPIC_SWORD_SISTER_OPEN = "(술잔을 내려다보며) 내 동생도 검을 들었었어. 너 정도 나이였지. …돌아오지 못했어.";
        public static string TEXT_TOPIC_SWORD_SISTER_REPLY_1 = "(아무 말 없이 옆에 앉는다)";
        public static string TEXT_TOPIC_SWORD_SISTER_ANSWER_1 = "…고마워. 아무 말 안 해 줘서. (한참 뒤) 너는 위험한 데 가지 마. 알았지?";
        public static string TEXT_TOPIC_SWORD_SISTER_REPLY_2 = "그래서 그렇게 앞에 서시는 거군요.";
        public static string TEXT_TOPIC_SWORD_SISTER_ANSWER_2 = "(고개를 돌린다) …눈치 빠른 거, 가끔 얄미워.";
        public static string TEXT_TOPIC_SWORD_AFTER_DAYOFF_OPEN = "(눈을 피한다) …어제 얘기, 다른 사람한테 하지 마. 내가 그렇게 많이 웃은 거.";
        public static string TEXT_TOPIC_SWORD_AFTER_DAYOFF_REPLY_1 = "저만 알게요.";
        public static string TEXT_TOPIC_SWORD_AFTER_DAYOFF_ANSWER_1 = "(작게) …그래. 너만.";
        public static string TEXT_TOPIC_SWORD_AFTER_DAYOFF_REPLY_2 = "웃는 거 보기 좋던데요.";
        public static string TEXT_TOPIC_SWORD_AFTER_DAYOFF_ANSWER_2 = "(얼굴이 붉어진다) 시끄러. 술이나 가져와.";

        // ── 마도사 ─────────────────────────────
        public static string TEXT_TOPIC_ELF_SPARK_OPEN = "(손끝에 작은 불꽃을 굴리며) 신기하지? 사람들은 마법을 무서워하거나, 아니면 돈을 내밀더라. 너는 어느 쪽이야?";
        public static string TEXT_TOPIC_ELF_SPARK_REPLY_1 = "그냥 예쁘네요.";
        public static string TEXT_TOPIC_ELF_SPARK_ANSWER_1 = "(불꽃이 툭 꺼진다) …어머. 그런 대답은 예상 못 했는데. 후후, 너 재밌다.";
        public static string TEXT_TOPIC_ELF_SPARK_REPLY_2 = "얼마면 되는데요?";
        public static string TEXT_TOPIC_ELF_SPARK_ANSWER_2 = "후후, 솔직해서 좋네. 비싸. 너는 못 낼걸?";
        public static string TEXT_TOPIC_ELF_YEARS_OPEN = "엘프는 오래 살아. 그래서 친해지는 게 무서워. 이 테이블 사람들도, 너도… 언젠가는 내가 먼저 보내야 하잖아.";
        public static string TEXT_TOPIC_ELF_YEARS_REPLY_1 = "그럼 오래오래 기억해 주세요.";
        public static string TEXT_TOPIC_ELF_YEARS_ANSWER_1 = "(눈을 크게 뜨더니 웃는다) …그거 좋다. 너는 특히 잘 기억해 둘게.";
        public static string TEXT_TOPIC_ELF_YEARS_REPLY_2 = "그래도 친해지는 게 낫지 않아요?";
        public static string TEXT_TOPIC_ELF_YEARS_ANSWER_2 = "말은 쉽지. …그런데 이미 늦은 것 같아. 이 사람들한테는.";
        public static string TEXT_TOPIC_ELF_OMEN_OPEN = "(갑자기 진지하게) 있잖아, 숲의 장로들이 그랬어. 십 년 안에 세상이 한 번 무너질 거래. …웃기지? 근데 너, 지금 안 웃었어.";
        public static string TEXT_TOPIC_ELF_OMEN_REPLY_1 = "그런 일, 안 일어나게 할 거예요.";
        public static string TEXT_TOPIC_ELF_OMEN_ANSWER_1 = "(가만히 들여다보다가) …'할 거예요'래. 막을 수 있는 사람처럼 말하네. 너, 정말 뭘 숨기고 있구나.";
        public static string TEXT_TOPIC_ELF_OMEN_REPLY_2 = "그냥 얘기잖아요.";
        public static string TEXT_TOPIC_ELF_OMEN_ANSWER_2 = "그렇지? 그냥 얘기야. (불꽃을 다시 굴린다) …그렇게 믿을게.";

        // ── 전사 ─────────────────────────────
        public static string TEXT_TOPIC_WARRIOR_SHIELD_OPEN = "(방패를 툭 친다) 이 녀석 말인가? 스무 해를 같이 굴렀지. 이 흠집들, 동료 대신 맞아 낸 흔적이라네.";
        public static string TEXT_TOPIC_WARRIOR_SHIELD_REPLY_1 = "그럼 이 방패가 제일 많이 다쳤겠네요.";
        public static string TEXT_TOPIC_WARRIOR_SHIELD_ANSWER_1 = "하하하! 그렇지! 그래서 내가 매일 닦아 주는 거라네. 자네, 말이 예쁘구먼.";
        public static string TEXT_TOPIC_WARRIOR_SHIELD_REPLY_2 = "무겁지 않으세요?";
        public static string TEXT_TOPIC_WARRIOR_SHIELD_ANSWER_2 = "무겁지. 가벼운 방패는 아무도 못 지켜.";
        public static string TEXT_TOPIC_WARRIOR_COUNT_OPEN = "(테이블을 둘러보며 목소리를 낮춘다) 저 녀석들, 다 내 자식뻘일세. 원정 나가면 맨 뒤에서 하나하나 세며 걷지. 다 있나, 다 있나.";
        public static string TEXT_TOPIC_WARRIOR_COUNT_REPLY_1 = "그래서 다들 돌아오는 거군요.";
        public static string TEXT_TOPIC_WARRIOR_COUNT_ANSWER_1 = "(눈가를 훔치며 웃는다) 이 사람, 늙은이 울리려고 작정했구먼.";
        public static string TEXT_TOPIC_WARRIOR_COUNT_REPLY_2 = "한 명이라도 모자라면요?";
        public static string TEXT_TOPIC_WARRIOR_COUNT_ANSWER_2 = "(웃음이 사라진다) …그 생각은 안 하려고 한다네. 안 하게 해 주게.";
        public static string TEXT_TOPIC_WARRIOR_FARM_OPEN = "사실 고향에 작은 밭이 있네. 언젠가 방패를 내려놓고 감자나 키울 생각이었지. …헌데 요즘은 그날이 자꾸 멀어지는군.";
        public static string TEXT_TOPIC_WARRIOR_FARM_REPLY_1 = "그날 오면 감자 사러 갈게요.";
        public static string TEXT_TOPIC_WARRIOR_FARM_ANSWER_1 = "하하! 약속했네! 자네한테는 제일 좋은 놈으로 골라 주지.";
        public static string TEXT_TOPIC_WARRIOR_FARM_REPLY_2 = "왜 멀어지는데요?";
        public static string TEXT_TOPIC_WARRIOR_FARM_ANSWER_2 = "이 녀석들을 두고 갈 수가 없어서지. 내가 없으면 누가 뒤에서 빠진 사람이 없는지 살피겠나.";

        // ── 요정 ─────────────────────────────
        public static string TEXT_TOPIC_FAIRY_HERBS_OPEN = "(작은 주머니를 흔들며) 이거 봐! 오늘 뒷산에서 캔 거야! 이건 열 내리는 거, 이건 상처 닫는 거, 이건… 그냥 냄새가 좋아!";
        public static string TEXT_TOPIC_FAIRY_HERBS_REPLY_1 = "냄새 좋은 것도 중요하죠.";
        public static string TEXT_TOPIC_FAIRY_HERBS_ANSWER_1 = "그치?! 아픈 사람은 냄새가 좋으면 덜 무서워해! 너 뭘 좀 아는구나!";
        public static string TEXT_TOPIC_FAIRY_HERBS_REPLY_2 = "그거 비싸게 팔리겠는데요.";
        public static string TEXT_TOPIC_FAIRY_HERBS_ANSWER_2 = "안 팔아! 다 우리 단 거야! …하나는 너 줄 수도 있고.";
        public static string TEXT_TOPIC_FAIRY_BEFORE_OPEN = "(날개를 접고 앉는다) 있잖아, 치유사는 다친 걸 낫게 하잖아. 근데 다치기 전에 막는 법은 몰라. 그게 제일 속상해.";
        public static string TEXT_TOPIC_FAIRY_BEFORE_REPLY_1 = "다치기 전엔 제가 밥이라도 든든히 챙길게요.";
        public static string TEXT_TOPIC_FAIRY_BEFORE_ANSWER_1 = "(눈이 반짝인다) 진짜? 그럼 너도 우리 단이야! 밥 담당!";
        public static string TEXT_TOPIC_FAIRY_BEFORE_REPLY_2 = "그건 방패 아저씨 일 아니에요?";
        public static string TEXT_TOPIC_FAIRY_BEFORE_ANSWER_2 = "아저씨도 다 못 막아. 그래서 내가 있는 거고… 근데 나도 다 못 고쳐.";
        public static string TEXT_TOPIC_FAIRY_SECRET_OPEN = "(귓속말하듯 날아와서) 비밀 하나 알려 줄까? 나 사실 무서워. 원정 갈 때마다. 근데 내가 무서워하면 다들 불안해할까 봐 웃는 거야.";
        public static string TEXT_TOPIC_FAIRY_SECRET_REPLY_1 = "무서울 땐 여기 와서 말해요. 여기선 안 웃어도 돼요.";
        public static string TEXT_TOPIC_FAIRY_SECRET_ANSWER_1 = "(한참 가만히 있다가, 작게) …응. 너한테는 그럴게.";
        public static string TEXT_TOPIC_FAIRY_SECRET_REPLY_2 = "웃는 게 더 힘들지 않아요?";
        public static string TEXT_TOPIC_FAIRY_SECRET_ANSWER_2 = "힘들어! 근데 그게 내 일이야. (그래도 조금 웃는다)";
        public static string TEXT_TOPIC_FAIRY_HEALED_OPEN = "(붕대 없는 팔을 흔들며) 이번엔 아무도 안 다쳤어! 누가 약값을 먼저 보내 줬대. 이름도 안 남기고! …너 혹시 알아?";
        public static string TEXT_TOPIC_FAIRY_HEALED_REPLY_1 = "좋은 사람이 있나 봐요.";
        public static string TEXT_TOPIC_FAIRY_HEALED_ANSWER_1 = "응! 언젠가 꼭 고맙다고 할 거야. 찾아내서!";
        public static string TEXT_TOPIC_FAIRY_HEALED_REPLY_2 = "제가 어떻게 알아요.";
        public static string TEXT_TOPIC_FAIRY_HEALED_ANSWER_2 = "(빤히 쳐다본다) …그치? 너는 여관 일꾼이니까. (그런데 계속 쳐다본다)";

        // 놓친 사정의 대가 뒤(2026-10-18, 과장 초안).
        public static string TEXT_TOPIC_FAIRY_WOUNDED_OPEN = "(붕대 감은 날개를 접으며) 날개가 이러니까 술잔도 못 날라 주겠다. …괜찮아, 금방 나아. 다들 내 앞에서 웃으려고 애쓰는 게 더 아파.";
        public static string TEXT_TOPIC_FAIRY_WOUNDED_REPLY_1 = "그럼 제가 대신 날라 드릴게요. 나을 때까지.";
        public static string TEXT_TOPIC_FAIRY_WOUNDED_ANSWER_1 = "(작게 웃는다) …그거 꽤 좋은 거래네. 약속이야. 다 나으면 내가 한 잔 살게.";
        public static string TEXT_TOPIC_FAIRY_WOUNDED_REPLY_2 = "약이 조금만 더 빨랐으면 좋았을 텐데요.";
        public static string TEXT_TOPIC_FAIRY_WOUNDED_ANSWER_2 = "(고개를 끄덕인다) 응. 누가 그걸 알았으면 좋았을 텐데. …이상하지, 너한테 그 말 들으니까 꼭 네가 미안해하는 것 같아.";

        public static string TEXT_TOPIC_SWORD_LOST_OPEN = "(빈 의자를 보며) 고드릭은 늘 저기 앉았어. 방패를 무릎에 세워 두고. …금이 간 걸 알면서도 아무 말 안 했지. 우리 다 알면서 아무 말 안 했어.";
        public static string TEXT_TOPIC_SWORD_LOST_REPLY_1 = "그 자리, 아무도 앉지 않게 해 둘게요.";
        public static string TEXT_TOPIC_SWORD_LOST_ANSWER_1 = "(한참 말이 없다가) …고마워. 이상하게 그 말이 제일 위로가 된다.";
        public static string TEXT_TOPIC_SWORD_LOST_REPLY_2 = "누구 탓도 아니에요.";
        public static string TEXT_TOPIC_SWORD_LOST_ANSWER_2 = "탓할 데가 없으니까 더 화가 나. (주먹을 쥐었다 편다) …미안. 너한테 할 말은 아니었어.";
    }
}
