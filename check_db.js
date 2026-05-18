const sql = require('mssql');

const config = {
    user: 'sa',
    password: 'SuperStrong!Passw0rd',
    server: 'localhost',
    database: 'GPMS_Desktop',
    options: {
        encrypt: false,
        trustServerCertificate: true
    }
};

async function checkData() {
    try {
        await sql.connect(config)
        let employees = await sql.query("SELECT * FROM [Employee];");
        console.log("EMPLOYEES:", employees.recordset);
    } catch (err) {
        console.error("SQL Error", err)
    } finally {
        process.exit();
    }
}
checkData()
