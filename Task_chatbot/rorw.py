class RorW:
  def __init__(self, ):
    # keywords
    self.special_keyowrd = ['沒錯']
    self.positive = ['是', '對', '行', '好', '恩', '有', '會', '同意', '可以']
    self.negative = ['沒', '錯', '不', '否', '才', '反']
    
  def get_right_or_wrong(self, text):
    if self.special_keyowrd[0] in text: return True
    for i in self.negative:
      if i in text:
        return False
    for i in self.positive:
      if i in text:
        return True
    return 'None' # if answer the unquestioned
