# Example: Reading File and Inserting Data into Database

## Step 1: Read the data file
```
read_file(
  path: "example-data.txt"
)
```

## Step 2: Parse the content and prepare for database insertion
The file contains client information in this format:
- Client Name: [Name]
- Email: [Email] 
- Phone: [Phone]

## Step 3: Insert into database using ore-tracking tools
```
ore_create_client(
  name: "Acme Corp",
  email: "contact@acme.com", 
  phone: "+1-555-0123"
)
```

## Step 4: Repeat for all clients in the file
```
ore_create_client(
  name: "Beta Ltd",
  email: "info@beta.com",
  phone: "+1-555-0456"
)

ore_create_client(
  name: "Gamma Inc", 
  email: "support@gamma.com",
  phone: "+1-555-0789"
)
```

## Complete workflow:
1. Use `read_file` to get the data
2. Parse the content in your application logic
3. Use `ore_create_client` for each client entry
4. The database will be updated automatically through the ore-tracking API

This demonstrates how tools can be chained together to read files and insert data into databases.