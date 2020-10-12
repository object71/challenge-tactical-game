<a href="https://www.buymeacoffee.com/object71" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" style="height: 60px !important;width: 217px !important;" ></a>

# Кратко описание
Unity 3D (използвайки C#) походова тактическа игра

# Правила

1. игралното поле е 2D grid от квадратчета (tiles) с фиксирани (за всяко ниво) размери, където всяко квадратче е или passable или impassable
2. играта се играе от 2ма играчи - Player и AI
3. всеки играч разполага няколко "единици" (units), като типа, броя и първоналното разположение на единиците са зададени предварително
4. всяка единица заема 1 квадратче от полето (не може да има повече от 1 единица в квадратче)
5. всяка единица може да се придвижва по полето използвайки movement points (MP) по следния начин:
  - хоризонтално / вертикално: 5 MP
  - диагонално: 7 MP
6. единиците могат да се придвижват само по passable tiles и НЕ могат да преминават през други единици
7. всяка единица (според типа си) има следните характеристики (всички са целочислени):
  - movement speed : колко MP може да "изхарчи" единицата за 1 ход
  - max health: колко "здраве" има първоначално единицата (колко щети сумарно може да понесе)
  - attack damage: колко щети нанася единицата при атака
  - attack range: на какво разстояние може да атакува единицата
  * разстоянието се задава в MP, т.е. мога да атакувам по там ако бих могъл да стигна до там с толкова MP
8. за един ход всяка единица може да се мести според колкото movement speed има и да атакува максимум веднъж
9. играчите се редуват на ходове (аз местя моите единици, после AI мести неговите)
  - хода приключва или ръчно (end turn button) или автоматично, когато нито една единица не може да се мести
10. при атака damage просто се вади от текущтия health на единицата и ако стане <= 0 - умира
11. печели този, който избие всички единици на противника
