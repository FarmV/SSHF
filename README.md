# SSHF  ScreenShotHelper by FarmV

Программа предпологается для использования скриншотов в виде визуальных закладок(пока одной). Ообенно актуально при использовании одного монитора, потому что нет необходимости постоянно сворачиватся. А также автоматизация пользовательского опыта.



#На данный момент обладает одним окном со следующими функциями:

- фоновая обработка клавиш, что позваляет вызывать функции  будучи свёрнутым в трей.

- О том что программа запущенна можно определить по иконке в трее. (Правый клик закрыть программу)


## Окно "Fast"

- Получение изображения из буффера обмена WIN + SHIFT + A.(Если буффер пуст, будет паказанно прошлое изображение или изображение по дефолту) 
Препологается совместное использование стандртного вызова скриншота в windows 10 через сочетания клавиш WIN + SHIFT + S.
- Остановка обновления положения окна, клавиша CTRL, или любой ввод мышью.
- Перетаскивание окна левая клавиша мыши.
- Маштабирование окна через прокрутку колёскика мыши.
- Закрытие окна через двойнойправый клик по форме. 
- Фиксация положения окна CTRL + CAPSLOCK.
- Извлечение сриншота в формате PNG из формы CTRL + MOUSEMOVE в проводник windows.

## Автоматизация
- Функция атоматизаии "перевод с экрана" всё ещё в тестовом режиме, клавиши 1+2+3.
Предпологается совместное использование ABBYY Screenshot Reader и Depl переводчик
десктопная версия. (Пока не реализованно окно выбора директорий программ. Хард код можно изменить класс App строки 199-200. Будет обавлино в ближайших обновлениях, но скорей всего не раньше чем произойдёт интеграции с базой данных)(P.S Не троготать мышь во время перевода, иначе возможен сбой алгоритма)

# В ближайщих обновления предполагаются:

- Итеграция с базой данных для выбора директорий программ
- Замена стандартного дефолтного изображеия. (Тестовое не несёт смыловой нагрузки)
- Добаление логатипа программы. (Cейчас 'тупо использет иконку node', если найдёт)
- После итеграции возможностm замены горячих клавиш. (Cоответсвуещая форма, и сохранение настроек в базе)
