using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Data;
using Mono.Data.Sqlite;
using System;
using System.IO;

public class DatabaseManager : MonoBehaviour
{

    // View
    public Text viewName, viewDescription, viewCost, viewQuantity, viewFilename;
    public RawImage viewImage;
    string dbFileName = "PokemonInventory.db";
    string dbDestination;
    public int currentCard = 0;
    public int numCards;
    public List<List<string>> cardList = new List<List<string>>();

    // Update
    public InputField updateName, updateDescription, updateCost, updateQuantity;
    public Text updateFilename;
    public RawImage updateImage;
    public GameObject noDataPanelUpate;

    // load inventory
    public GameObject inventoryHolder, contentHolder;

    // total order
    public Text confirmOrder;
    string placedOrder;

    // place order
    string screenShotURL = "http://unityjumpstart.com/phpform/OrderProcess.php";
    public InputField buyerName, buyerAddress, buyerCity, buyerState, buyerZip, buyerEmail;
    public GameObject noDataConfirm;
    public Transform thankYou;

    // code cleanup
    public GameObject noDataPanelBuy;
    public Transform confirm;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(RunDBCode(dbFileName));
    }

    IEnumerator RunDBCode(string fileName)
    {
        dbDestination = Path.Combine(Application.persistentDataPath, "data");
        dbDestination = Path.Combine(dbDestination, fileName);

        if (!File.Exists(dbDestination))
        {
            string dbStreamingAsset = Path.Combine(Application.streamingAssetsPath, fileName);
            byte[] result;

            if (dbStreamingAsset.Contains("://") || dbStreamingAsset.Contains(":///"))
            {
                WWW www = new WWW(dbStreamingAsset);
                yield return www;
                result = www.bytes;
            }
            else
            {
                result = File.ReadAllBytes(dbStreamingAsset);
            }

            Debug.Log("Loaded db file");

            if (!Directory.Exists(Path.GetDirectoryName(dbDestination)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dbDestination));
            }

            File.WriteAllBytes(dbDestination, result);
            Debug.Log("Copied db file");
        }
        try
        {
            Debug.Log("DB Path: " + dbDestination.Replace("/", "\\"));
            dbDestination = "URI=file:" + dbDestination;
            SqliteConnection connection = new SqliteConnection(dbDestination);
            connection.Open();
            IDbCommand cmnd_read = connection.CreateCommand();
            IDataReader reader;
            string query1 = "SELECT COUNT(*) FROM Cards";
            cmnd_read.CommandText = query1;
            reader = cmnd_read.ExecuteReader();
                
            while(reader.Read())
            {
                numCards = int.Parse(reader[0].ToString());
            }
            reader.Close();
            string query2 = "SELECT * FROM Cards";
            cmnd_read.CommandText = query2;
            reader = cmnd_read.ExecuteReader();

            while (reader.Read())
            {
                List<string> tempList = new List<string>();
                tempList.Add(reader[0].ToString()); // name
                tempList.Add(reader[1].ToString()); // description
                tempList.Add(reader[2].ToString()); // cost
                tempList.Add(reader[3].ToString()); // quantity
                tempList.Add(reader[4].ToString()); // filename

                cardList.Add(tempList);
            }
            connection.Close();
        }
        catch(Exception e)
        {
            Debug.Log("Failed: " + e.Message);
        }
        DrawCardsOnScreen();
        LoadInventory();
    }

    void DrawCardsOnScreen()
    {
        viewName.text = cardList[currentCard][0].ToString();
        viewDescription.text = "Description: " + cardList[currentCard][1].ToString();
        viewCost.text = "Cost: " + cardList[currentCard][2].ToString();
        viewQuantity.text = "Quantity: " + cardList[currentCard][3].ToString();
        viewFilename.text = "Filename: " + cardList[currentCard][4].ToString();

        updateName.text = cardList[currentCard][0].ToString();
        updateDescription.text = cardList[currentCard][1].ToString();
        updateCost.text = cardList[currentCard][2].ToString();
        updateQuantity.text = cardList[currentCard][3].ToString();
        updateFilename.text = "Filename: " + cardList[currentCard][4].ToString();

        string filePath = Application.streamingAssetsPath + "/" + cardList[currentCard][4].ToString();
        WWW www = new WWW(filePath);

        while(!www.isDone)
        {}
        viewImage.texture = www.texture;
        updateImage.texture = www.texture;
    }

    public void NextCard()
    {
        currentCard++;

        if(currentCard > (cardList.Count - 1))
        {
            currentCard = 0;
        }
        DrawCardsOnScreen();
    }

    public void PreviousCard()
    {
        currentCard--;

        if(currentCard < 0)
        {
            currentCard = cardList.Count - 1;
        }
        DrawCardsOnScreen();
    }

    public void UpdateSingleCard()
    {
        if (string.IsNullOrWhiteSpace(updateDescription.text) ||
            string.IsNullOrWhiteSpace(updateCost.text) ||
            string.IsNullOrWhiteSpace(updateQuantity.text))
        {
            noDataPanelUpate.SetActive(true);
            return;
        }
        SqliteConnection connection = new SqliteConnection(dbDestination);
        connection.Open();
        IDbCommand command = connection.CreateCommand();
        // The methods shown in the video caused a lot of errors around the updated text
        // Did some research and found this way of doing it using parameters
        command.CommandText = "UPDATE Cards SET Name = @name, Description = @desc, Cost = @cost, Quantity = @qty WHERE NAME = " + "\"" + cardList[currentCard][0].ToString() + "\"";
        SqliteParameter nameParam = new SqliteParameter("@name", updateName.text);
        SqliteParameter descParam = new SqliteParameter("@desc", updateDescription.text);
        SqliteParameter costParam = new SqliteParameter("@cost", updateCost.text);
        SqliteParameter qtyParam = new SqliteParameter("@qty", updateQuantity.text);
        command.Parameters.Add(nameParam);
        command.Parameters.Add(descParam);
        command.Parameters.Add(costParam);
        command.Parameters.Add(qtyParam);
        command.ExecuteNonQuery();
        connection.Close();
        cardList = new List<List<string>>();
        GameObject[] inventoryItems = GameObject.FindGameObjectsWithTag("Inventory Item");
        foreach (GameObject gameObject in inventoryItems)
        {
            Destroy(gameObject);
        }
        StartCoroutine(RunDBCode(dbFileName));
    }

    public void ClosePanelUpdate()
    {
        noDataPanelUpate.SetActive(false);
    }

    public void LoadInventory()
    {
        for(int i = 0; i < cardList.Count; i++)
        {
            GameObject temp = Instantiate(inventoryHolder);
            temp.transform.SetParent(contentHolder.transform, false);
            temp.transform.Find("Text-CardName").GetComponent<Text>().text = "Name: " + cardList[i][0].ToString();
            temp.transform.Find("Text-Description").GetComponent<Text>().text =cardList[i][1].ToString();
            List<string> dropdownOptions = new List<string>();
            temp.transform.Find("Dropdown").GetComponent<Dropdown>().ClearOptions();
            for(int j=0; j<= int.Parse(cardList[i][3]); j++)
            {
                dropdownOptions.Add(j.ToString());
            }
            temp.transform.Find("Dropdown").GetComponent<Dropdown>().AddOptions(dropdownOptions);
            temp.transform.Find("Text-Cost").GetComponent<Text>().text = "Cost: " + cardList[i][2].ToString();
            string filePath = Application.streamingAssetsPath + "/" + cardList[i][4].ToString();
            WWW www = new WWW(filePath);
            while (!www.isDone)
            {}
            temp.transform.Find("RawImage-Picture").GetComponent<RawImage>().texture = www.texture;
        }
    }

    public void TotalOrder()
    {
        GameObject[] inventoryItems = GameObject.FindGameObjectsWithTag("Inventory Item");
        string tempOrder = "Card Order Information" + "\n";
        float orderSubTotal = 0.0f;
        float orderTax = .0825f;
        float orderTotal = 0.0f;

        foreach(GameObject gameObject in inventoryItems)
        {
            int tempQuantity = gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().value;

            if(tempQuantity > 0)
            {
                tempOrder += "\n";
                tempOrder += gameObject.transform.Find("Text-CardName").GetComponent<Text>().text;
                tempOrder += "\n";
                tempOrder += "Quantity: " + tempQuantity + "\n";
                string tempCost = gameObject.transform.Find("Text-Cost").GetComponent<Text>().text;
                tempCost = tempCost.Replace("Cost: ", "");
                float tempSubTotal = (tempQuantity * float.Parse(tempCost));
                orderSubTotal += tempSubTotal;
                tempOrder += "Card Subtotal: " + tempQuantity + " x " + tempCost + " = " + tempSubTotal;
                tempOrder += "\n";
            }
        }
        tempOrder += "\n";
        tempOrder += "Order Subtotal: " + orderSubTotal.ToString(".00");
        tempOrder += "\n";
        float tempTax = orderSubTotal * orderTax;
        tempOrder += "Order Tax: " + tempTax.ToString(".00");
        tempOrder += "\n";
        orderTotal = orderSubTotal + tempTax;
        tempOrder += "Order Total: " + orderTotal.ToString(".00");

        if (orderTotal <= 0)
        {
            noDataPanelBuy.SetActive(true);
            return;
        }
        confirmOrder.text = tempOrder;
        placedOrder = tempOrder;
        GameObject.Find("Camera Holder").GetComponent<CameraMove>().setAnchor(confirm);
    }

    public void PlaceOrder()
    {
        if (string.IsNullOrWhiteSpace(buyerName.text) ||
            string.IsNullOrWhiteSpace(buyerAddress.text) ||
            string.IsNullOrWhiteSpace(buyerCity.text) ||
            string.IsNullOrWhiteSpace(buyerState.text) ||
            string.IsNullOrWhiteSpace(buyerZip.text) ||
            string.IsNullOrWhiteSpace(buyerEmail.text))
        {
            noDataConfirm.SetActive(true);
            return;
        }
        else
        {
            GameObject.Find("Camera Holder").GetComponent<CameraMove>().setAnchor(thankYou);
        }
        GameObject[] inventoryItems = GameObject.FindGameObjectsWithTag("Inventory Item");

        for(int i = 0; i < inventoryItems.Length; i++)
        {
            int tempQuanity = inventoryItems[i].gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().value;
            int dbQuantity = int.Parse(cardList[i][3]);
            int newQuantity;
            if (tempQuanity > 0)
            {
                newQuantity = dbQuantity - tempQuanity;
            }
            else
            {
                newQuantity = dbQuantity;
            }
            SqliteConnection connection = new SqliteConnection(dbDestination);
            connection.Open();
            IDbCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE Cards SET Quantity = @qty WHERE NAME = " + "\"" + cardList[i][0].ToString() + "\"";
            SqliteParameter qtyParam = new SqliteParameter("@qty", newQuantity);
            command.Parameters.Add(qtyParam);
            command.ExecuteNonQuery();
            connection.Close();
        }
        cardList = new List<List<string>>();

        foreach (GameObject gameObject in inventoryItems)
        {
            Destroy(gameObject);
        }
        StartCoroutine(RunDBCode(dbFileName));
        placedOrder = placedOrder.Replace("\n", "<br>");
        screenShotURL += "?order=" + placedOrder;
        screenShotURL += "&name=" + buyerName.text;
        screenShotURL += "&address=" + buyerAddress.text;
        screenShotURL += "&city=" + buyerCity.text;
        screenShotURL += "&state=" + buyerState.text;
        screenShotURL += "&zip=" + buyerZip.text;
        screenShotURL += "&email=" + buyerEmail.text;
        Application.OpenURL(screenShotURL);
        screenShotURL = "http://unityjumpstart.com/phpform/OrderProcess.php";
        buyerName.text = "";
        buyerAddress.text = "";
        buyerCity.text = "";
        buyerState.text = "";
        buyerZip.text = "";
        buyerEmail.text = "";
    }

    public void ClosePanelConfirm()
    {
        noDataConfirm.SetActive(false);
    }

    public void ClosePanelBuy()
    {
        noDataPanelBuy.SetActive(false);
    }
}
