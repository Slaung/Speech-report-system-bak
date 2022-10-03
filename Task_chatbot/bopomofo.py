from pypinyin import pinyin, Style
style = Style.BOPOMOFO

class Bopomofo:
    def bopomofo(self,speech):
        # 載入所有data
        alldataF = open('/大三專題/task_chatbot6/database/alldata.txt', 'r', encoding='utf-8')
        alldata=[] # 所有txt檔
        for i in alldataF.readlines():
            i=i.replace('\n', '')
            alldata.append(i)
        alldataF.close()
        # 載入所有data的注音
        BopomofoF = open('/大三專題/task_chatbot6/database/alldataBopomofo.txt', 'r', encoding='utf-8')
        alldataBopomofo = []
        for i in BopomofoF.readlines():
            i=i.replace('\n', '')
            dataBopomofolist = i.split("/")
            alldataBopomofo.append(dataBopomofolist)
        BopomofoF.close()
        
        #自定義字典(處理破音字)
        dic = {'興': 'ㄒㄧㄥ', '彈': 'ㄊㄢˊ', '仔': 'ㄗ˙', '思': 'ㄙˋ', '識': 'ㄕˋ', '倒': 'ㄉㄠˇ'}
        #常見的口齒不清
        dic2 = {'ㄒ': 'ㄑ', 'ㄑ': 'ㄒ', 'ㄣ': 'ㄥ', 'ㄥ': 'ㄣ', 'ㄈ': 'ㄏ', 'ㄏ': 'ㄈ', 'ㄓ': 'ㄗ', 'ㄗ': 'ㄓ', 'ㄚ': 'ㄤ', 'ㄤ': 'ㄚ', 'ㄙ': 'ㄕ', 'ㄕ': 'ㄙ'}
        
        speechBopomofolist=[]
        for i in range(len(speech)):
            check=1
            for key in dic:
                if(speech[i]==key):
                    speechBopomofolist.append(dic[key])
                    check=0
            if check:
                s = pinyin(speech[i], Style.BOPOMOFO)
                speechBopomofolist.extend([i for item in s for i in item])
        alter=[] #存放speech已換過字的index
        
        #1. speech裡面有跟address_data或是disaster_case_data相同時，該index存進alter
        for data in alldata:
            if speech.find(data) != -1:
                for i in range(speech.find(data),speech.find(data)+len(data)):
                    alter.append(i)
        alter = list(set(alter)) #去重複
        
        #2. 同音不同字處理
        for i in range(len(speech)):
            if i not in alter:
                for index in range(len(alldataBopomofo)):
                    dataBopomofolist = alldataBopomofo[index]
                    found=0
                    #判斷同音不同字
                    if speechBopomofolist[i] == dataBopomofolist[0]:
                        start=i
                        for j in range(len(dataBopomofolist)):
                            if ( (len(speech)-i+1<=len(dataBopomofolist)) or (speechBopomofolist[i+j] != dataBopomofolist[j]) ):
                                found=0
                                break
                            else:
                                found=1
                    if found :
                        for k in range(start,start+len(dataBopomofolist)):
                            data = alldata[index]
                            speech = speech.replace(speech[k], data[k-start]) #把speech裡錯的字改正
                            alter.append(k) # 把改正過字的存起來
        
        #3. 口齒不清更改注音
        for i in range(len(speech)):
            if i not in alter:
                for key2 in dic2: 
                    speechBopomofolist2 = speechBopomofolist[:] # 載入speech原始的讀音，防止原讀音被更改
                    if key2 in speechBopomofolist2[i]:
                        speechBopomofolist2[i] = speechBopomofolist2[i].replace(key2, dic2[key2]) # 替換注音
                        for index in range(len(alldataBopomofo)):
                            dataBopomofolist = alldataBopomofo[index]
                            found=0
                            for ii in range(len(speech)):
                                if speechBopomofolist2[ii] == dataBopomofolist[0]:
                                    start=ii
                                    for j in range(len(dataBopomofolist)):
                                        if ( (len(speech)-ii+1<=len(dataBopomofolist)) or (speechBopomofolist2[ii+j] != dataBopomofolist[j]) ):
                                            found=0
                                            break
                                        else:
                                            found=1
                                if found :
                                    for k in range(start,start+len(dataBopomofolist)):
                                        data = alldata[index]
                                        speech = speech.replace(speech[k], data[k-start]) #把speech裡錯的字改正
                                        alter.append(k) # 把改正過字的存起來
        return speech