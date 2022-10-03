import gensim
import jieba
import numpy as np

class Case:
  def __init__(self, ):
    self.model = gensim.models.Word2Vec.load('/大三專題/task_chatbot8/database/word2vec_data/word2vec.model')
    
    self.catch_animals = []
    self.fire = []
    self.first_aid = []
    self.trap = []

    f = open('/大三專題/task_chatbot8/database/disaster_case_data/抓動物.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.catch_animals.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/disaster_case_data/火災.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.fire.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/disaster_case_data/緊急救護.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.first_aid.append(i.replace('\n', ''))
    f.close()

    f = open('/大三專題/task_chatbot8/database/disaster_case_data/受困.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.trap.append(i.replace('\n', ''))
    f.close()

    self.qakeyword1 = []
    f = open('/大三專題/task_chatbot8/database/disaster_case_data/QAKeyword.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.qakeyword1.append(i.replace('\n', ''))
    f.close()

    self.qakeyword2 = []
    f = open('/大三專題/task_chatbot8/database/disaster_case_data/QAKeyword2.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.qakeyword2.append(i.replace('\n', ''))
    f.close()

    self.qakeyword3 = []
    f = open('/大三專題/task_chatbot8/database/disaster_case_data/QAKeyword3.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.qakeyword3.append(i.replace('\n', ''))
    f.close()

    self.keyword_dicts = [self.trap, self.catch_animals,  self.fire, self.first_aid]
    ###
    jieba.set_dictionary('/大三專題/task_chatbot8/database/jieba_dict_data/dict.txt')
    jieba.load_userdict('/大三專題/task_chatbot8/database/jieba_dict_data/mydict.txt')
    ###
    self.weight2case = {
        0:'受困',
        1:'抓動物',
        2:'火災',
        3:'緊急救護'}
    
  def __jieba_tokenizer(self, text):
    words = jieba.cut(text)
    return [word for word in words]

  def __get_simwords(self, words):
    '''
    :words => jiebaed text
    :sim_dict => # word: [sim_rate, keyword]
    '''
    sim_dict = dict()# word: [sim_rate, keyword]
    for keyword_dict in self.keyword_dicts:
      for keyword in keyword_dict:
        for word in words:
          try: sim_rate = self.model.similarity(word, keyword)
          except: sim_rate = 0
          try:
            if sim_dict[word][0] < sim_rate:
              sim_dict[word] = [sim_rate, keyword]
          except: sim_dict[word]=[0, '']
    return sim_dict

  def __get_keywords(self, sim_dict):
    simwords_val = np.array(list(sim_dict.values()))
    keywords = []
    for sim_rate, keyword in simwords_val:
      if sim_rate == simwords_val[simwords_val.argmax(axis=0)[0]][0]:
        keywords.append(keyword)
    return keywords

  def get_case(self, keywords): # word similarity based
    keyword_weight = []
    for keyword in keywords:
      for id, keyword_dict in enumerate(self.keyword_dicts):
        if keyword in keyword_dict:
          keyword_weight.append(id)
          break
    try:
      return self.weight2case[min(keyword_weight)]
    except: return self.weight2case[0]

  def get_case2(self, words): # some rule (based on word similarity)
    grades = [0,0,0,0]

    for word in words:
      for idx, keyword_dict in enumerate(self.keyword_dicts):
        if word in keyword_dict:
          grades[idx] += 2
          break
        else:
          for keyword in keyword_dict:
            try:
              if model.similarity(word, keyword) > 0.6:
                grades[idx] += 1
                break
            except: pass

    maxidx = grades.index(max(grades))
    rgrades = list(reversed(grades))
    rmaxidx = rgrades.index(max(rgrades))

    if maxidx == (3-rmaxidx):
      #print(grades)
      #print(self.weight2case[np.argmax(grades)])
      return self.weight2case[np.argmax(grades)]
    else:
      #print('pass')
      return 'pass'

  def predict(self, text):
    words = self.__jieba_tokenizer(text)

    pred = self.get_case2(words)
    if pred != 'pass':
      flag = 0
      return pred, flag
    else:
      flag = 1
      sim_dict = self.__get_simwords(words)
      keywords = self.__get_keywords(sim_dict)
      pred = self.get_case(keywords)
      return pred, flag
  def get_aid_keyword(self, text):
    '''
    QAKeyword1 => 不舒服 => 請問是有人不舒服嗎
    QAKeyword2 => 受傷 => 請問是有人受傷嗎
    QAKeyword3 => 其他 => 請問是有人不舒服還是受傷嗎
    '''
    qa = ['請問是有人不舒服嗎', '請問是有人受傷嗎', '請問是有人不舒服還是受傷嗎']
    max_sim_cls = 0
    max_sim_rate = 0

    words = self.__jieba_tokenizer(text)
    for word in words:
      for idx, dict_ in enumerate([self.qakeyword1, self.qakeyword2, self.qakeyword3]):
        for keyword in dict_:
          try:
            sim_rate = self.model.similarity(word, keyword)
            if sim_rate > max_sim_rate:
              max_sim_cls = idx
              max_sim_rate = sim_rate
          except: pass
    return qa[max_sim_cls], max_sim_rate
      
