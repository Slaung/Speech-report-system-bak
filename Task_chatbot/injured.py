import gensim
import jieba

class Injured:
  def __init__(self, ):
    self.model = gensim.models.Word2Vec.load('/大三專題/task_chatbot8/database/word2vec_data/word2vec.model')

    self.first_aid = [] # rescue
    f = open('/大三專題/task_chatbot8/database/disaster_case_data/緊急救護.txt', 'r', encoding='utf-8')
    for i in f.readlines():
      self.first_aid.append(i.replace('\n', ''))
    f.close()

  def __jieba_tokenizer(self, text):
    words = jieba.cut(text)
    return [word for word in words]

  def __get_num_key(self, words):
    '''
    :words: jiebaed sentence
    :index: num_key's index
    '''
    unit_key = ['位', '個', '人', '名', '輛', '台']
    index = set()
    for id, word in enumerate(words):
      if word.isnumeric():
        index.add(id)
      elif word[-1] in unit_key and word[:-1].isnumeric():
        index.add(id)
    return list(index)

  def __get_rescue_key(self, words):
    '''
    :words: jiebaed sentence
    :index: rescue_key's index
    '''
    key, index, x = '', -1, 0
    for id, word in enumerate(words):
      for j in self.first_aid:
        try:
          if self.model.similarity(word, j)>x:
            x = self.model.similarity(word, j)
            key = j
            index = id
        except: pass
    return index, key

  def get_num_injured(self, text):
    words = self.__jieba_tokenizer(text)
    index = self.__get_num_key(words)
    res_i, res_k = self.__get_rescue_key(words)

    min = 100
    idx = 100
    for i in index:
      if abs(res_i-i)<min:
        min = abs(res_i-i)
        idx = i
    unit_key = ['位', '個', '人', '名', '輛', '台']
    try:
      for i in words[idx+1]:
        if i in unit_key:
          if '幾' in words[idx+1]:
            return words[idx] + '幾'
          elif '幾' in words[idx-1]:
            return '幾' + words[idx]
          else: return words[idx]
    except:
      if '幾' in text: return '幾'
      else: return ''
