class Address:
  def __init__(self, ):
    self.township = []
    self.rd = []
    self.st = []
    self.other = []
    self.ln = []

    f = open('/大三專題/task_chatbot8/database/address_data/township.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.township.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/address_data/rd.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.rd.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/address_data/st.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.st.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/address_data/other.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.other.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/address_data/ln.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.ln.append(i.replace('\n', ''))
    f.close()
    
  def __get_township(self, text):
    ts = []
    
    for i in self.township:
      if i[:2] in text:
        ts.append(i)

    return ts[-1]

  def __get_street(self, text):
    total = [self.st, self.rd, self.other]
    if '街' in text: flag = 0
    elif '路' in text: flag = 1
    else: flag = 2

    for i in total[flag]:
      if i[:3] in text:
        return i
    return ''

  def __get_lane(self, text):
    index = text.index('巷')
    
    for i in self.ln:
      if i in text: return i

    x=1
    while(text[index-x:index].isnumeric()):
      x+=1
    index += 1
    
    return text[index-x:index]

  def __get_section(self, text):
    index = text.index('段')

    x=1
    while(text[index-x:index].isnumeric()):
      x+=1
    index += 1
    
    return text[index-x:index]
    
  def __get_alley(self, text):
    index = text.index('弄')

    x=1
    while(text[index-x:index].isnumeric()):
      x+=1
    index += 1
    
    return text[index-x:index]

  def __get_number(self, text):
    flag = 1
    
    try: text_ = text.replace('之', '-')
    except: text_ = text

    if '-' in text_:
      flag = 0
      text_ = text_.replace('-', '')
    
    if not flag:
      index = text_.index('號')

      x=1
      while(text_[index-x:index].isnumeric()):
        x+=1
      index += 1
      try: return text[index-x:index+1].replace('之', '-')
      except: text[index-x:index+1]
    else:
      index = text.index('號')

      x=1
      while(text[index-x:index].isnumeric()):
        x+=1
      index += 1
      return text[index-x:index]

  def __get_floor(self, text):
    index = text.index('樓')

    x=1
    while(text[index-x:index].isnumeric()):
      x+=1
    index += 1
    
    return text[index-x:index]

  def get_address(self, text):
    address = {'鄉鎮市': '',
                '街路': '',
                '段': '',
                '巷': '',
                '弄': '',
                '號': '',
                '樓': ''}

    try:
      address['鄉鎮市'] = self.__get_township(text)
    except: pass
    
    try:
      address['街路'] = self.__get_street(text)
    except: pass
    
    if '段' in text:
      address['段'] = self.__get_section(text)
    
    if '巷' in text:
      address['巷'] = self.__get_lane(text)
    
    if '弄' in text:
      address['弄'] = self.__get_alley(text)
    
    if '號' in text:
      address['號'] = self.__get_number(text)
    
    if '樓' in text:
      address['樓'] = self.__get_floor(text)

    return address
