namespace VelaSeed;

/// <summary>
/// NCPDP SCRIPT 20170715 XML message builders.
/// These templates are proven to work against the test Function App.
/// Do not simplify or remove fields — the backend validates XML structure.
/// </summary>
public static class XmlTemplates
{
    public static string Build(SeedMessage seed, Provider p, string messageId, string sentTime, string password) =>
        seed.MessageType switch
        {
            "NewRx" => NewRx(p, messageId, sentTime, password),
            "CancelRx" => CancelRx(p, messageId, sentTime, password),
            "RxRenewalRequest" => RxRenewalRequest(p, messageId, sentTime, password),
            "RxChangeRequest" => RxChangeRequest(p, messageId, sentTime, password),
            _ => throw new NotSupportedException($"No template for: {seed.MessageType}")
        };

    static string NewRx(Provider p, string id, string time, string pw) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Message DatatypesVersion=""20170715"" TransportVersion=""20170715"" TransactionDomain=""SCRIPT"" TransactionVersion=""20170715"" StructuresVersion=""20170715"" ECLVersion=""20170715"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:noNamespaceSchemaLocation=""transport.xsd"">
    <Header>
        <To Qualifier=""P"">{p.PharmacyNcpdpId}</To>
        <From Qualifier=""D"">{p.Vpi}</From>
        <MessageID>{id}</MessageID>
        <SentTime>{time}</SentTime>
        <Security>
            <UsernameToken>
                <Password Type=""PasswordDigest"">String</Password>
                <Created>2001-12-17T09:30:47Z</Created>
            </UsernameToken>
            <Sender>
                <SecondaryIdentification>{pw}</SecondaryIdentification>
                <TertiaryIdentification>111112121221288</TertiaryIdentification>
            </Sender>
        </Security>
        <SenderSoftware>
            <SenderSoftwareDeveloper>MDLITE</SenderSoftwareDeveloper>
            <SenderSoftwareProduct>443</SenderSoftwareProduct>
            <SenderSoftwareVersionRelease>2.1</SenderSoftwareVersionRelease>
        </SenderSoftware>
        <DigitalSignature Version=""1.1"">
            <DigitalSignatureIndicator>true</DigitalSignatureIndicator>
        </DigitalSignature>
    </Header>
    <Body>
        <NewRx>
            <BenefitsCoordination>
                <CardholderID>CARDHOLDERID</CardholderID>
            </BenefitsCoordination>
            <Patient>
                <HumanPatient>
                    <Identification></Identification>
                    <Name>
                        <LastName>SMITH</LastName>
                        <FirstName>MARY</FirstName>
                    </Name>
                    <Gender>F</Gender>
                    <DateOfBirth>
                        <Date>1954-12-25</Date>
                    </DateOfBirth>
                    <Address>
                        <AddressLine1>45 EAST ROAD SW</AddressLine1>
                        <City>CLANCY</City>
                        <StateProvince>WI</StateProvince>
                        <PostalCode>54999</PostalCode>
                        <CountryCode>US</CountryCode>
                    </Address>
                </HumanPatient>
            </Patient>
            <Pharmacy>
                <Identification>
                    <NCPDPID>{p.PharmacyNcpdpId}</NCPDPID>
                    {p.PharmacyNpiXml}
                </Identification>
                <BusinessName>HUMANA</BusinessName>
                <Address>
                    <AddressLine1>7789 Smackers Lane</AddressLine1>
                    <City>Inver Grove Heights</City>
                    <StateProvince>MN</StateProvince>
                    <PostalCode>55117</PostalCode>
                    <CountryCode>US</CountryCode>
                </Address>
                <CommunicationNumbers>
                    <PrimaryTelephone>
                        <Number>7179758659</Number>
                    </PrimaryTelephone>
                </CommunicationNumbers>
            </Pharmacy>
            <Prescriber>
                <NonVeterinarian>
                    <Identification>
                        <DEANumber>{p.PrescriberDea}</DEANumber>
                        <NPI>{p.PrescriberNpi}</NPI>
                    </Identification>
                    <Name>
                        <LastName>ALLEN</LastName>
                        <FirstName>AARON</FirstName>
                    </Name>
                    <Address>
                        <AddressLine1>211 CENTRAL ROAD</AddressLine1>
                        <City>JONESVILLE</City>
                        <StateProvince>MN</StateProvince>
                        <PostalCode>37777</PostalCode>
                        <CountryCode>US</CountryCode>
                    </Address>
                    <CommunicationNumbers>
                        <PrimaryTelephone>
                            <Number>6152219800</Number>
                        </PrimaryTelephone>
                    </CommunicationNumbers>
                </NonVeterinarian>
            </Prescriber>
            <MedicationPrescribed>
                <DrugDescription>KADIAN ER 10 MG CAPSULE</DrugDescription>
                <DrugCoded>
                    <ProductCode>
                        <Code>00023601160</Code>
                        <Qualifier>ND</Qualifier>
                    </ProductCode>
                    <Strength>
                        <StrengthValue>240</StrengthValue>
                        <StrengthForm>
                            <Code>C42998</Code>
                        </StrengthForm>
                        <StrengthUnitOfMeasure>
                            <Code>C28253</Code>
                        </StrengthUnitOfMeasure>
                    </Strength>
                    <DEASchedule>
                        <Code>C48675</Code>
                    </DEASchedule>
                </DrugCoded>
                <Quantity>
                    <Value>60</Value>
                    <CodeListQualifier>38</CodeListQualifier>
                    <QuantityUnitOfMeasure>
                        <Code>C28253</Code>
                    </QuantityUnitOfMeasure>
                </Quantity>
                <WrittenDate>
                    <Date>{DateTime.UtcNow:yyyy-MM-dd}</Date>
                </WrittenDate>
                <Substitutions>0</Substitutions>
                <NumberOfRefills>0</NumberOfRefills>
                <DrugCoverageStatusCode>SI</DrugCoverageStatusCode>
                <Sig>
                    <SigText>Methadone HCl Oral Solution 10mg/mL Cherry Bottle 1000mlBt</SigText>
                </Sig>
            </MedicationPrescribed>
        </NewRx>
    </Body>
</Message>";

    static string CancelRx(Provider p, string id, string time, string pw) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Message DatatypesVersion=""20170715"" TransportVersion=""20170715"" TransactionDomain=""SCRIPT"" TransactionVersion=""20170715"" StructuresVersion=""20170715"" ECLVersion=""20170715"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:noNamespaceSchemaLocation=""transport.xsd"">
    <Header>
        <To Qualifier=""P"">{p.PharmacyNcpdpId}</To>
        <From Qualifier=""D"">{p.Vpi}</From>
        <MessageID>{id}</MessageID>
        <RelatesToMessageID>pw{p.Vpi[..8]}orig001</RelatesToMessageID>
        <SentTime>{time}</SentTime>
        <Security>
            <UsernameToken>
                <Password Type=""PasswordDigest"">String</Password>
                <Created>2020-07-16T09:30:47Z</Created>
            </UsernameToken>
            <Sender>
                <SecondaryIdentification>{pw}</SecondaryIdentification>
                <TertiaryIdentification>111112121221288</TertiaryIdentification>
            </Sender>
        </Security>
        <SenderSoftware>
            <SenderSoftwareDeveloper>EMRSoftware </SenderSoftwareDeveloper>
            <SenderSoftwareProduct>10</SenderSoftwareProduct>
            <SenderSoftwareVersionRelease>2</SenderSoftwareVersionRelease>
        </SenderSoftware>
        <PrescriberOrderNumber>213232323232323223</PrescriberOrderNumber>
    </Header>
    <Body>
        <CancelRx>
            <Patient>
                <HumanPatient>
                    <Name>
                        <LastName>SMITH</LastName>
                        <FirstName>MARY</FirstName>
                    </Name>
                    <Gender>F</Gender>
                    <DateOfBirth>
                        <Date>1954-12-25</Date>
                    </DateOfBirth>
                </HumanPatient>
            </Patient>
            <Prescriber>
                <NonVeterinarian>
                    <Identification>
                        <NPI>{p.PrescriberNpi}</NPI>
                    </Identification>
                    <Name>
                        <LastName>ALLEN</LastName>
                        <FirstName>AARON</FirstName>
                    </Name>
                    <CommunicationNumbers>
                        <PrimaryTelephone>
                            <Number>6152219800</Number>
                        </PrimaryTelephone>
                    </CommunicationNumbers>
                </NonVeterinarian>
            </Prescriber>
            <MedicationPrescribed>
                <DrugDescription>KADIAN ER 10 MG CAPSULE</DrugDescription>
                <DrugCoded>
                    <ProductCode>
                        <Code>00023601160</Code>
                        <Qualifier>ND</Qualifier>
                    </ProductCode>
                    <Strength>
                        <StrengthValue>240</StrengthValue>
                        <StrengthForm>
                            <Code>C42998</Code>
                        </StrengthForm>
                        <StrengthUnitOfMeasure>
                            <Code>C28253</Code>
                        </StrengthUnitOfMeasure>
                    </Strength>
                </DrugCoded>
                <Quantity>
                    <Value>60</Value>
                    <CodeListQualifier>38</CodeListQualifier>
                    <QuantityUnitOfMeasure>
                        <Code>C48542</Code>
                    </QuantityUnitOfMeasure>
                </Quantity>
                <DaysSupply>30</DaysSupply>
                <WrittenDate>
                    <Date>{DateTime.UtcNow:yyyy-MM-dd}</Date>
                </WrittenDate>
                <Substitutions>0</Substitutions>
                <NumberOfRefills>1</NumberOfRefills>
                <Sig>
                    <SigText>TAKE ONE TABLET TWO TIMES A DAY UNTIL GONE</SigText>
                </Sig>
            </MedicationPrescribed>
        </CancelRx>
    </Body>
</Message>";

    static string RxRenewalRequest(Provider p, string id, string time, string pw) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Message DatatypesVersion=""20170715"" TransportVersion=""20170715"" TransactionDomain=""SCRIPT"" TransactionVersion=""20170715"" StructuresVersion=""20170715"" ECLVersion=""20170715"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:noNamespaceSchemaLocation=""transport.xsd"">
    <Header>
        <To Qualifier=""D"">{p.Vpi}</To>
        <From Qualifier=""P"">{p.PharmacyNcpdpId}</From>
        <MessageID>{id}</MessageID>
        <SentTime>{time}</SentTime>
        <Security>
            <UsernameToken>
                <Password Type=""PasswordDigest"">String</Password>
                <Created>2001-12-17T09:30:47Z</Created>
            </UsernameToken>
            <Sender>
                <SecondaryIdentification>{pw}</SecondaryIdentification>
                <TertiaryIdentification>111112121221288</TertiaryIdentification>
            </Sender>
        </Security>
        <SenderSoftware>
            <SenderSoftwareDeveloper>ACE SOFTWARE</SenderSoftwareDeveloper>
            <SenderSoftwareProduct>ACE1</SenderSoftwareProduct>
            <SenderSoftwareVersionRelease>1.1</SenderSoftwareVersionRelease>
        </SenderSoftware>
        <RxReferenceNumber>PW{id[..Math.Min(id.Length, 10)]}</RxReferenceNumber>
        <PrescriberOrderNumber>4444444</PrescriberOrderNumber>
    </Header>
    <Body>
        <RxRenewalRequest>
            <Patient>
                <HumanPatient>
                    <Identification></Identification>
                    <Name>
                        <LastName>SMITH</LastName>
                        <FirstName>MARY</FirstName>
                    </Name>
                    <Gender>F</Gender>
                    <DateOfBirth>
                        <Date>1954-12-25</Date>
                    </DateOfBirth>
                </HumanPatient>
            </Patient>
            <Pharmacy>
                <Identification>
                    <NCPDPID>{p.PharmacyNcpdpId}</NCPDPID>
                    {p.PharmacyNpiXml}
                </Identification>
                <BusinessName>MAIN STREET PHARMACY</BusinessName>
                <Address>
                    <AddressLine1>5400 S 121 ST</AddressLine1>
                    <City>HALES CORNERS</City>
                    <StateProvince>TN</StateProvince>
                    <PostalCode>37122</PostalCode>
                    <CountryCode>US</CountryCode>
                </Address>
                <CommunicationNumbers>
                    <PrimaryTelephone>
                        <Number>6152205656</Number>
                    </PrimaryTelephone>
                </CommunicationNumbers>
            </Pharmacy>
            <Prescriber>
                <NonVeterinarian>
                    <Identification>
                        {p.PrescriberNpiXml}
                    </Identification>
                    <Name>
                        <LastName>ALLEN</LastName>
                        <FirstName>AARON</FirstName>
                    </Name>
                    <Address>
                        <AddressLine1>211 CENTRAL ROAD</AddressLine1>
                        <City>JONESVILLE</City>
                        <StateProvince>TN</StateProvince>
                        <PostalCode>37777</PostalCode>
                    </Address>
                    <CommunicationNumbers>
                        <PrimaryTelephone>
                            <Number>6152219800</Number>
                        </PrimaryTelephone>
                    </CommunicationNumbers>
                </NonVeterinarian>
            </Prescriber>
            <MedicationDispensed>
                <DrugDescription>KADIAN ER 10 MG CAPSULE</DrugDescription>
                <DrugCoded>
                    <ProductCode>
                        <Code>00023601160</Code>
                        <Qualifier>ND</Qualifier>
                    </ProductCode>
                    <Strength>
                        <StrengthValue>300</StrengthValue>
                        <StrengthForm>
                            <Code>C42998</Code>
                        </StrengthForm>
                        <StrengthUnitOfMeasure>
                            <Code>C28253</Code>
                        </StrengthUnitOfMeasure>
                    </Strength>
                </DrugCoded>
                <Quantity>
                    <Value>60</Value>
                    <CodeListQualifier>38</CodeListQualifier>
                    <QuantityUnitOfMeasure>
                        <Code>C48542</Code>
                    </QuantityUnitOfMeasure>
                </Quantity>
                <DaysSupply>30</DaysSupply>
                <LastFillDate>
                    <Date>{DateTime.UtcNow.AddDays(-30):yyyy-MM-dd}</Date>
                </LastFillDate>
                <Substitutions>0</Substitutions>
                <Sig>
                    <SigText>TAKE ONE CAPSULE BY MOUTH TWICE DAILY</SigText>
                </Sig>
                <PharmacyRequestedRefills>4</PharmacyRequestedRefills>
            </MedicationDispensed>
            <MedicationPrescribed>
                <DrugDescription>KADIAN ER 10 MG CAPSULE</DrugDescription>
                <DrugCoded>
                    <Strength>
                        <StrengthValue>300</StrengthValue>
                        <StrengthForm>
                            <Code>C42998</Code>
                        </StrengthForm>
                        <StrengthUnitOfMeasure>
                            <Code>C28253</Code>
                        </StrengthUnitOfMeasure>
                    </Strength>
                </DrugCoded>
                <Quantity>
                    <Value>60</Value>
                    <CodeListQualifier>38</CodeListQualifier>
                    <QuantityUnitOfMeasure>
                        <Code>C48542</Code>
                    </QuantityUnitOfMeasure>
                </Quantity>
                <DaysSupply>30</DaysSupply>
                <WrittenDate>
                    <Date>{DateTime.UtcNow:yyyy-MM-dd}</Date>
                </WrittenDate>
                <Substitutions>0</Substitutions>
                <NumberOfRefills>2</NumberOfRefills>
                <Sig>
                    <SigText>TAKE ONE CAPSULE BY MOUTH TWICE DAILY</SigText>
                </Sig>
            </MedicationPrescribed>
            <FollowUpPrescriber>
                <NonVeterinarian>
                    <Identification>
                        {p.PrescriberNpiXml}
                    </Identification>
                    <Name>
                        <LastName>ALLEN</LastName>
                        <FirstName>AARON</FirstName>
                    </Name>
                </NonVeterinarian>
            </FollowUpPrescriber>
        </RxRenewalRequest>
    </Body>
</Message>";

    static string RxChangeRequest(Provider p, string id, string time, string pw) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Message DatatypesVersion=""20170715"" TransportVersion=""20170715"" TransactionDomain=""SCRIPT"" TransactionVersion=""20170715"" StructuresVersion=""20170715"" ECLVersion=""20170715"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:noNamespaceSchemaLocation=""transport.xsd"">
    <Header>
        <To Qualifier=""D"">{p.Vpi}</To>
        <From Qualifier=""P"">{p.PharmacyNcpdpId}</From>
        <MessageID>{id}</MessageID>
        <RelatesToMessageID>pw{p.Vpi[..8]}orig002</RelatesToMessageID>
        <SentTime>{time}</SentTime>
        <Security>
            <UsernameToken>
                <Password Type=""PasswordDigest"">String</Password>
                <Created>2001-12-17T09:30:47Z</Created>
            </UsernameToken>
            <Sender>
                <SecondaryIdentification>{pw}</SecondaryIdentification>
                <TertiaryIdentification>111112121221288</TertiaryIdentification>
            </Sender>
        </Security>
        <SenderSoftware>
            <SenderSoftwareDeveloper>MDLITE</SenderSoftwareDeveloper>
            <SenderSoftwareProduct>443</SenderSoftwareProduct>
            <SenderSoftwareVersionRelease>2.1</SenderSoftwareVersionRelease>
        </SenderSoftware>
        <RxReferenceNumber>PH888</RxReferenceNumber>
        <PrescriberOrderNumber>110088</PrescriberOrderNumber>
    </Header>
    <Body>
        <RxChangeRequest>
            <MessageRequestCode>G</MessageRequestCode>
            <Patient>
                <HumanPatient>
                    <Name>
                        <LastName>SMITH</LastName>
                        <FirstName>MARY</FirstName>
                    </Name>
                    <Gender>F</Gender>
                    <DateOfBirth>
                        <Date>1954-12-25</Date>
                    </DateOfBirth>
                    <Address>
                        <AddressLine1>45 EAST ROAD SW</AddressLine1>
                        <City>CLANCY</City>
                        <StateProvince>WI</StateProvince>
                        <PostalCode>54999</PostalCode>
                    </Address>
                    <CommunicationNumbers>
                        <PrimaryTelephone>
                            <Number>6515550122</Number>
                        </PrimaryTelephone>
                    </CommunicationNumbers>
                </HumanPatient>
            </Patient>
            <Pharmacy>
                <Identification>
                    <NCPDPID>{p.PharmacyNcpdpId}</NCPDPID>
                    {p.PharmacyNpiXml}
                </Identification>
                <BusinessName>HUMANA</BusinessName>
                <CommunicationNumbers>
                    <PrimaryTelephone>
                        <Number>7179758659</Number>
                    </PrimaryTelephone>
                </CommunicationNumbers>
            </Pharmacy>
            <Prescriber>
                <NonVeterinarian>
                    <Identification>
                        <NPI>{p.PrescriberNpi}</NPI>
                    </Identification>
                    <Name>
                        <LastName>ALLEN</LastName>
                        <FirstName>AARON</FirstName>
                    </Name>
                    <Address>
                        <AddressLine1>211 CENTRAL ROAD</AddressLine1>
                        <City>JONESVILLE</City>
                        <StateProvince>MN</StateProvince>
                        <PostalCode>37777</PostalCode>
                    </Address>
                    <CommunicationNumbers>
                        <PrimaryTelephone>
                            <Number>6152219800</Number>
                        </PrimaryTelephone>
                    </CommunicationNumbers>
                </NonVeterinarian>
            </Prescriber>
            <MedicationPrescribed>
                <DrugDescription>CALAN SR 240 mg TABLET, EXTENDED RELEASE</DrugDescription>
                <DrugCoded>
                    <ProductCode>
                        <Code>00143950910</Code>
                        <Qualifier>ND</Qualifier>
                    </ProductCode>
                    <Strength>
                        <StrengthValue>240</StrengthValue>
                        <StrengthForm>
                            <Code>C42998</Code>
                        </StrengthForm>
                        <StrengthUnitOfMeasure>
                            <Code>C28253</Code>
                        </StrengthUnitOfMeasure>
                    </Strength>
                </DrugCoded>
                <Quantity>
                    <Value>60</Value>
                    <CodeListQualifier>38</CodeListQualifier>
                    <QuantityUnitOfMeasure>
                        <Code>C48542</Code>
                    </QuantityUnitOfMeasure>
                </Quantity>
                <DaysSupply>30</DaysSupply>
                <WrittenDate>
                    <Date>{DateTime.UtcNow:yyyy-MM-dd}</Date>
                </WrittenDate>
                <Substitutions>0</Substitutions>
                <NumberOfRefills>1</NumberOfRefills>
                <Note>SUBSTITUTE GENERIC</Note>
                <Sig>
                    <SigText>TAKE ONE TABLET TWO TIMES A DAY UNTIL GONE</SigText>
                </Sig>
            </MedicationPrescribed>
            <MedicationRequested>
                <DrugDescription>VERAPAMIL HCL 240 MG TABLET, EXTENDED RELEASE</DrugDescription>
                <DrugCoded>
                    <ProductCode>
                        <Code>00143950910</Code>
                        <Qualifier>ND</Qualifier>
                    </ProductCode>
                </DrugCoded>
                <Quantity>
                    <Value>60</Value>
                    <CodeListQualifier>38</CodeListQualifier>
                    <QuantityUnitOfMeasure>
                        <Code>C48542</Code>
                    </QuantityUnitOfMeasure>
                </Quantity>
                <DaysSupply>30</DaysSupply>
                <Substitutions>0</Substitutions>
                <NumberOfRefills>1</NumberOfRefills>
                <Sig>
                    <SigText>TAKE ONE TABLET TWO TIMES A DAY UNTIL GONE</SigText>
                </Sig>
            </MedicationRequested>
            <FollowUpPrescriber>
                <NonVeterinarian>
                    <Identification>
                        <NPI>{p.PrescriberNpi}</NPI>
                    </Identification>
                    <Name>
                        <LastName>ALLEN</LastName>
                        <FirstName>AARON</FirstName>
                    </Name>
                </NonVeterinarian>
            </FollowUpPrescriber>
        </RxChangeRequest>
    </Body>
</Message>";
}
