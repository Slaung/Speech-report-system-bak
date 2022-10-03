class Zh2Digit:
  def __init__(self, ):
    self.zh2digit_table = {
      '零' : 0,
      '一' : 1, '二' : 2, '三' : 3, '四' : 4, '五' : 5, '六' : 6, '七' : 7, '八' : 8, '九' : 9,
      '兩' : 2, '十' : 10, '百' : 100, '千' : 1000, 
      '0' : 0, '1' : 1, '2' : 2, '3' : 3, '4' : 4, '5' : 5, '6' : 6, '7' : 7, '8' : 8, '9' : 9,
      '10' : 10, '100' : 100, '1000' : 1000
    }
  def zh2digit(self, zh_num):
    # 位數遞增，由高位開始取
    digit_num = 0
    # 結果
    result = 0
    # 暫時存儲的變量
    tmp = 0
    # 億的個數
    billion = 0
    while digit_num < len(zh_num):
      tmp_zh = zh_num[digit_num]
      tmp_num = self.zh2digit_table.get(tmp_zh, None)
      if tmp_num == 100000000:
          result = result + tmp
          result = result * tmp_num
          billion = billion * 100000000 + result
          result = 0
          tmp = 0
      elif tmp_num == 10000:
          result = result + tmp
          result = result * tmp_num
          tmp = 0
      elif tmp_num >= 10:
          if tmp == 0:
              tmp = 1
          result = result + tmp_num * tmp
          tmp = 0
      elif tmp_num is not None:
          tmp = tmp * 10 + tmp_num
      digit_num += 1
    result = result + tmp
    result = result + billion
    return result
  
  def zh2digit_text(self, text):
    result_digit = []
    result_zh = []
    now_result_digit = ''
    now_result_zh = ''
    for word in text:
      if word.isnumeric():
        now_result_zh += word
      else:
        now_result_digit = self.zh2digit(now_result_zh)
        if now_result_zh:
          result_digit.append(now_result_digit)
          result_zh.append(now_result_zh)
        now_result_zh = ''
    for i in range(len(result_zh)):
      text = text.replace(result_zh[i], str(result_digit[i]))

    return text, result_digit, result_zh
