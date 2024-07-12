project.Variables["debug_log"].Value = "";

try
{
    // 1. Turcane listA
    List<string> tempList = new List<string>(project.Lists["listAccs"]);
    if (tempList.Count > 0)
    {
        tempList.RemoveAt(0);
    }
    for (int i = tempList.Count - 1; i >= 0; i--)
    {
        if (string.IsNullOrWhiteSpace(tempList[i]))
        {
            tempList.RemoveAt(i);
        }
    }

    project.Variables["debug_log"].Value += $"После очистки tempList: Count = {tempList.Count}\n";

    // Получение значений диапазона
    project.Variables["debug_log"].Value += $"var_tempA = {project.Variables["var_IndexStart"].Value}, var_tempB = {project.Variables["var_tempB"].Value}\n";
    
    int startIndex, endIndex;
    if (!int.TryParse(project.Variables["var_IndexStart"].Value, out startIndex))
    {
        throw new FormatException($"Некорректное значение var_tempA: {project.Variables["var_IndexStart"].Value}");
    }
    if (!int.TryParse(project.Variables["var_tempB"].Value, out endIndex))
    {
        throw new FormatException($"Некорректное значение var_tempB: {project.Variables["var_tempB"].Value}");
    }
    
    startIndex -= 1;

    project.Variables["debug_log"].Value += $"Диапазон: startIndex = {startIndex}, endIndex = {endIndex}\n";

    // Применяем фильтрацию по диапазону
    tempList = tempList.GetRange(startIndex, Math.Min(endIndex - startIndex, tempList.Count - startIndex));

    project.Variables["debug_log"].Value += $"После фильтрации по диапазону: tempList.Count = {tempList.Count}\n";
    project.Variables["debug_log"].Value += "Элементы в списке после фильтрации:\n";
    for (int i = 0; i < tempList.Count; i++)
    {
        project.Variables["debug_log"].Value += $"{i}: {tempList[i]}\n";
    }

    // 2. lenListA
    project.Variables["var_checkLen"].Value = tempList.Count.ToString();

    List<string> listC = new List<string>();

    while (true)
    {
        project.Variables["debug_log"].Value += "Начало итерации\n";
        
        if (tempList.Count == 0)
        {
            project.Variables["util_alldone"].Value = "True";
            project.Variables["debug_log"].Value += "Не найден подходящий аккаунт\n";
            throw new Exception("Не найден подходящий аккаунт");
        }

        // 3. get random acc0
        Random rnd = new Random();
        int randomIndex = rnd.Next(tempList.Count);
        string randomAccount = tempList[randomIndex];
        project.Variables["acc0"].Value = randomAccount;
        project.Variables["debug_log"].Value += $"acc0 установлен: {randomAccount}\n";
        
        tempList.RemoveAt(randomIndex);

        // 4. get acc0 status & cooldown
        int acc0Index;
        if (!int.TryParse(project.Variables["acc0"].Value, out acc0Index))
        {
            throw new FormatException($"Некорректное значение acc0: {project.Variables["acc0"].Value}");
        }
        project.Variables["debug_log"].Value += $"acc0Index: {acc0Index}\n";

        if (acc0Index >= 0 && acc0Index < project.Lists["listStatuses"].Count && acc0Index < project.Lists["listSleep"].Count)
        {
            project.Variables["var_status"].Value = project.Lists["listStatuses"][acc0Index];
            project.Variables["time_t00"].Value = project.Lists["listSleep"][acc0Index];
            project.Variables["debug_log"].Value += $"Статус: {project.Variables["var_status"].Value}, Время: {project.Variables["time_t00"].Value}\n";
        }
        else
        {
            project.Variables["debug_log"].Value += "Нет данных для аккаунта, возврат true\n";
            return true;
        }

        // 5. check done work skip fail
        if (project.Variables["var_status"].Value != "done" &&
            project.Variables["var_status"].Value != "work" &&
            project.Variables["var_status"].Value != "skip" &&
            (project.Variables["util_DoFail"].Value == "True" || project.Variables["var_status"].Value != "fail"))
        {
            // 6. check cooldown
            if (!string.IsNullOrEmpty(project.Variables["time_t00"].Value))
            {
                project.Variables["time_t01"].Value = DateTime.Now.ToString("MM.dd.yyyy HH:mm:ss");
                project.Variables["debug_log"].Value += $"time_t00: {project.Variables["time_t00"].Value}, time_t01: {project.Variables["time_t01"].Value}\n";

                // Конвертация строковых значений в DateTime
                DateTime startTime, endTime;
                if (!DateTime.TryParseExact(project.Variables["time_t00"].Value, "MM.dd.yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out startTime))
                {
                    throw new FormatException($"Некорректный формат времени time_t00: {project.Variables["time_t00"].Value}");
                }
                if (!DateTime.TryParseExact(project.Variables["time_t01"].Value, "MM.dd.yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out endTime))
                {
                    throw new FormatException($"Некорректный формат времени time_t01: {project.Variables["time_t01"].Value}");
                }

                // Вычисление разницы во времени в секундах
                int timeDifferenceInSeconds = (int)(endTime - startTime).TotalSeconds;

                project.Variables["vartDiffer"].Value = timeDifferenceInSeconds.ToString();
                project.Variables["debug_log"].Value += $"timeDifferenceInSeconds: {timeDifferenceInSeconds}\n";

                if (timeDifferenceInSeconds <= 0)
                {
                    listC.Add(project.Variables["acc0"].Value);
                    project.Variables["debug_log"].Value += "Аккаунт добавлен в listC из-за отрицательной разницы во времени\n";
                    continue;
                }
            }
            //LOG
            project.Variables["var_last"].Value = $"starting... ";
            project.SendToLog($"#{project.Variables["acc0"].Value} {project.Variables["var_last"].Value}", ZennoLab.InterfacesLibrary.Enums.Log.LogType.Info, true, ZennoLab.InterfacesLibrary.Enums.Log.LogColor.Default);

            project.Variables["debug_log"].Value += "Аккаунт прошел все проверки, возврат true\n";
            return true;
        }
        else
        {
            listC.Add(project.Variables["acc0"].Value);
            project.Variables["debug_log"].Value += "Аккаунт добавлен в listC из-за статуса\n";
        }

        // 8. check Lens
        if (project.Variables["var_checkLen"].Value == listC.Count.ToString())
        {
            project.Variables["util_alldone"].Value = "True";
            project.Variables["debug_log"].Value += "Не найден подходящий аккаунт\n";
            throw new Exception("Не найден подходящий аккаунт");
        }
    }
}
catch (Exception ex)
{
    project.Variables["debug_error"].Value = $"Ошибка: {ex.GetType().Name} - {ex.Message}";
    project.Variables["debug_acc0"].Value = project.Variables["acc0"].Value;
    project.Variables["debug_listA"].Value = string.Join(", ", project.Lists["listAccs"]);
    project.Variables["debug_var_tempA"].Value = project.Variables["var_IndexStart"].Value;
    project.Variables["debug_var_tempB"].Value = project.Variables["var_tempB"].Value;
    throw;
}
