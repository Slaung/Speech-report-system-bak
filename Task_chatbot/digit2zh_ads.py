class Digit2Zh_ads():
  def __init__(self, ):    
    self.ads_zh = []
    self.ads_digit = []

    f = open('/大三專題/task_chatbot8/database/address_data/township.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.ads_zh.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/address_data/rd.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.ads_zh.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/address_data/st.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.ads_zh.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/address_data/other.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.ads_zh.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/address_data/ln.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.ads_zh.append(i.replace('\n', ''))
    f.close()
    
    f = open('/大三專題/task_chatbot8/database/address_data/township_.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.ads_digit.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/address_data/rd_.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.ads_digit.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/address_data/st_.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.ads_digit.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/address_data/other_.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.ads_digit.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/address_data/ln_.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.ads_digit.append(i.replace('\n', ''))
    f.close()
    
  def digit2zh(self, text):
    for i in self.ads_digit:
      if i in text:
        idx = self.ads_digit.index(i)
        text = text.replace(i, self.ads_zh[idx])
    return text
