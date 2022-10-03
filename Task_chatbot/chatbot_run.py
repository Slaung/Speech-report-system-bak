from chatbot_init import *

#@title QQ
while(True):
  mode = input('choose mode (run or interrupt)') # don't reload library
  if mode == 'run':

    #@title 2. zh2digit
    def zh2digit(text):
      t = 0
      R = True
      zh2digit_flag = False
      while t<len(text) and R:
        try:
          if text[t].isnumeric():
            pass
          else:
            R = False
            zh2digit_flag = True
            break
          t += 1
        except: break
      ###

      if zh2digit_flag:
        output = z2d.zh2digit_text(text)[0]
      else: output = str(z2d.zh2digit(text))
      
      return output


    #@title 3. first
    def first(text):
      global goal
      
      # update address
      text = d2z_ads.digit2zh(text)
      address = ads.get_address(text) # dict
      for key, val in address.items():
        if val: goal['address'][key] = val
        # replace address to ''
        if key in ['鄉鎮市', '街路']:
          text = text.replace(val[:-1], '')
        else: text = text.replace(val, '')
      
      # update case
      case = cas.predict(text)
      goal['case'] = case
      
      # update number of injured
      injured_num = inj.get_num_injured(text)
      if injured_num:
        goal['injured_num'] = str(injured_num)


    #@title 4. second

    # intent => according to preqrestion
    def second(text, intent):
      global goal

      # update address
      if intent == 'address':
        text = d2z_ads.digit2zh(text)
        address = ads.get_address(text) # dict
        for key, val in address.items():
          if val: goal['address'][key] = val

      # update case
      elif intent == 'case':
        case = cas.predict(text)
        goal['case'] = case

      # update number of injured
      elif intent == 'injured':
        injured_num = inj.get_num_injured(text)
        if injured_num:
          goal['injured_num'] = str(injured_num)


    #@title 5. response
    def response():
      global address_first_flag
      global address_flag
      global case_flag
      global injured_flag
      global goal

      # case response
      if not case_flag:
        return '請問發生什麼事', 'case'

      # address first time response
      if not address_first_flag:
        #address_first_flag = True
        return '請問地點在哪', 'address'
      
      # address second time response
      if not address_flag:
        if not goal['address']['鄉鎮市']:
          return '請問在苗栗縣的哪個地區', 'address'
        elif not goal['address']['街路']:
          return '請問是' + str(goal['address']['鄉鎮市']) + '的哪條路', 'address'
        elif not goal['address']['號']:
          return '請問在幾號附近', 'address'

      # number of injured response
      if not injured_flag:
        goal['injured_num'] = '0'
        return '請問有多少人需要救護車請講大概數字', 'injured'


    #@title 6. update_address_first_flag
    def update_address_first_flag():
      global address_first_flag
      global goal

      for val in goal['address'].values():
        if val:
          address_first_flag = True


    #@title 7. rorw_response
    def rorw_response(text):
      global goal
      global resp
      global fire_trap_falg

      # response address (True or False)
      address = ''
      if goal['address']['鄉鎮市'] and goal['address']['街路'] and goal['address']['號'] and not address_flag:
        address = ''.join(list(goal['address'].values()))
        #for val in goal['address'].values():
        #  address += str(val)
        resp.append('請問地點是'+ address +'嗎')

      # response case (True or False)
      elif goal['case'] and not case_flag:
        '''
        if goal['case'] == '緊急救護':
          keyword, _ = fak.get_keyword(text)
          resp.append('請問是' + keyword + '嗎')
        else:
          case_resp = ['請問發生的是火災嗎', '請問是有人不舒服還是受傷嗎', '請問是有動物需要抓捕嗎', '請問是有人受困嗎']
          case2index = ['火災', '緊急救護', '抓動物', '受困']
          resp.append(case_resp[case2index.index(goal['case'][0])])
        '''
        qa, _ = cas.get_aid_keyword(text)
        case_resp = ['請問發生的是火災嗎', qa, '請問是有動物需要抓捕嗎', '請問是有人受困嗎']
        case2index = ['火災', '緊急救護', '抓動物', '受困']
        resp.append(case_resp[case2index.index(goal['case'][0])])
      # response number of injured (True or False)
      elif goal['injured_num'] and not injured_flag:
        # to solve user don't know how many number of injured in 'fire' and 'trap'
        if goal['case'][0] in ['火災', '受困'] and goal['injured_num'] is '0':##
          goal['injured_num'] = '1'
          fire_trap_falg = True
        else:
          if goal['injured_num'] == '0':
            resp.append('不用救護車對嗎')
          else:
            injured_resp = '請問是' + str(goal['injured_num']) + '人需要救護車嗎'
            digit2zh = {
                '10':'十', '20':'二十', '30':'三十', '40':'四十', '50':'五十', '60':'六十', '70':'七十', '80':'八十', '90':'九十', 
                '100': '百'
            }
            if '幾' in injured_resp:
              for key, val in digit2zh.items():
                if key in injured_resp: injured_resp = injured_resp.replace(key, val)
            resp.append(injured_resp)
      
      # update flag
      intent_ = []
      if goal['address']['鄉鎮市'] and goal['address']['街路'] and goal['address']['號'] and not address_flag:
        intent_.append('address')
      if goal['case'] and not case_flag:
        intent_.append('case')
      if goal['injured_num'] and not injured_flag:
        intent_.append('injured')
     
      return resp.pop(0), intent_


    #@title 8. update_or_not
    def update_or_not(text, intnet_):
      global goal
      global address_flag
      global case_flag
      global injured_flag
     
      bool_ = rorw.get_right_or_wrong(text)

      if bool_ == None:
        return 'continue'
      
      # update address and flag
      if 'address' in intnet_:
        intnet_.pop(intnet_.index('address'))
        if  not bool_:
          goal['address'] = {'鄉鎮市': '', '街路': '', '段': '', '巷': '', '弄': '', '號': '', '樓': ''}
          address_first_flag = False
        else:
          address_flag = True

      # update case and flag
      elif 'case' in intnet_:
        intnet_.pop(intnet_.index('case'))
        if not bool_:
          goal['case'] = ''
        else:
          case_flag = True
      
      # update number of injured and flag
      elif 'injured' in intnet_:
        intnet_.pop(intnet_.index('injured'))
        if bool_:
          injured_flag = True
        if goal['injured_num']:
          injured_flag = True
        if not bool_:
          goal['injured_num'] = ''
          injured_flag = False
      
      return intnet_


    #@title 9. end_form_goal
    def end_from_goal():
      global goal
      if address_flag and case_flag and injured_flag:
        return True
      else: return False
    # to count
    counter = 0

    # set goal list
    goal = {
        'address':{
            '鄉鎮市': '', #*
            '街路': '', #*
            '段': '',
            '巷': '',
            '弄': '',
            '號': '', #*
            '樓': ''
        },
        'case': '',
        'injured_num': ''
    }

    # if false then representative this not confirmed
    address_flag = False
    case_flag = False
    injured_flag = False

    # token
    address_first_flag = False
    #update_goal_list_flag = True
    fire_trap_falg = False
    resp = []

    max_round = 20 # need to discuss

    text  = ''
    text_ = ''

    while(True):
      # end from max round
      if counter == max_round:
        print('chatbot: ' + '請不要惡作劇')
        goal = {
            'address':{
                '鄉鎮市': '', #*
                '街路': '', #*
                '段': '',
                '巷': '',
                '弄': '',
                '號': '', #*
                '樓': ''},
            'case': '',
            'injured_num': ''}
        break

      # update goal list
      #if update_goal_list_flag:
      output, intent = response()
      text = input('chatbot: ' + output + '\nuser:    ')

      ### to solve no input
      if not text: continue
      ###
      
      text = zh2digit(text)
      if not counter:
        first(text)
      else:
        second(text, intent)

        # update token
        update_address_first_flag()
      #update_goal_list_flag = True # to test this

      # confirm the answer
      while(True):
        try:
          output_, intent_ = rorw_response(text)
          text_ = input('chatbot: ' + output_ + '\nuser:    ')

          ### to solve no input
          if not text_: continue
          ###
          
          text_ = zh2digit(text_)
          intent_ = update_or_not(text_, intent_)
          if intent_ == 'continue':
            break
          '''
          if len(text_)>1: # to test this. => if case then update goal list else pass
            second(text, intent)
            update_goal_list_flag = False
          '''
          if not intent_: break
        except: break

      # print goal list
      #print('round : {}'.format(counter))
      #print(goal)
      #print('\n')


      # end from goal
      if end_from_goal() or fire_trap_falg:
        print('chatbot: ' + '感謝你的報案')
        break
      # end from keyword
      if text == 'end': break
      
      # to count
      counter += 1

    # Should be considered 
    if '幾' in goal['injured_num']: goal['injured_num'] = goal['injured_num'].replace('幾', '')

    print('final goal =>address: {}\n\t     case: {}\n\t     number of injured: {}'.format(
        ''.join(list(goal['address'].values())), goal['case'][0], goal['injured_num']))
    
  elif mode == 'interrupt': break
