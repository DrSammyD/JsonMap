<?xml version="1.0" encoding="utf-8"?>
<configurationSectionModel xmlns:dm0="http://schemas.microsoft.com/VisualStudio/2008/DslTools/Core" dslVersion="1.0.0.0" Id="4744f52c-8c2b-4598-b110-6c5024d479d7" namespace="JsonMap" xmlSchemaNamespace="urn:JsonMap" assemblyName="JsonMap" xmlns="http://schemas.microsoft.com/dsltools/ConfigurationSectionDesigner">
  <typeDefinitions>
    <externalType name="String" namespace="System" />
    <externalType name="Boolean" namespace="System" />
    <externalType name="Int32" namespace="System" />
    <externalType name="Int64" namespace="System" />
    <externalType name="Single" namespace="System" />
    <externalType name="Double" namespace="System" />
    <externalType name="DateTime" namespace="System" />
    <externalType name="TimeSpan" namespace="System" />
  </typeDefinitions>
  <configurationElements>
    <configurationSection name="JsonMapSection" codeGenOptions="Singleton, XmlnsProperty" xmlSectionName="jsonMapSection">
      <elementProperties>
        <elementProperty name="JsonMapQueryer" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="jsonMapQueryer" isReadOnly="false">
          <type>
            <configurationElementMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/JsonMapQueryer" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationSection>
    <configurationElement name="JsonMapQueryer">
      <attributeProperties>
        <attributeProperty name="JsonMapQueryerType" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="jsonMapQueryerType" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="SavedMapFileName" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="savedMapFileName" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
      <elementProperties>
        <elementProperty name="ValidationClasses" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="validationClasses" isReadOnly="false">
          <type>
            <configurationElementCollectionMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/ValidationClasses" />
          </type>
        </elementProperty>
        <elementProperty name="MappingAssemblies" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="mappingAssemblies" isReadOnly="false">
          <type>
            <configurationElementCollectionMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/MappingAssemblies" />
          </type>
        </elementProperty>
        <elementProperty name="MappingNamespaces" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="mappingNamespaces" isReadOnly="false">
          <type>
            <configurationElementCollectionMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/MappingNamespaces" />
          </type>
        </elementProperty>
        <elementProperty name="JSTypeEnumConfig" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="jSTypeEnumConfig" isReadOnly="false">
          <type>
            <configurationElementMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/JSTypeEnumConfig" />
          </type>
        </elementProperty>
        <elementProperty name="JsonMapEnumConfig" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="jsonMapEnumConfig" isReadOnly="false">
          <type>
            <configurationElementMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/JsonMapEnumConfig" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationElement>
    <configurationElement name="JSTypeEnumConfig">
      <attributeProperties>
        <attributeProperty name="ObservableArrayEnum" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="observableArrayEnum" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/Int64" />
          </type>
        </attributeProperty>
        <attributeProperty name="EnumType" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="enumType" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="PrimativeEnum" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="primativeEnum" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/Int64" />
          </type>
        </attributeProperty>
        <attributeProperty name="ViewModelEnum" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="viewModelEnum" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/Int64" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="JsonMapEnumConfig">
      <attributeProperties>
        <attributeProperty name="EnumType" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="enumType" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="DefaultMapEnum" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="defaultMapEnum" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/Int64" />
          </type>
        </attributeProperty>
        <attributeProperty name="InheritedMapEnum" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="inheritedMapEnum" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/Int64" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElementCollection name="ValidationClasses" xmlItemName="validationClass" codeGenOptions="Indexer, AddMethod, RemoveMethod, GetItemMethods">
      <attributeProperties>
        <attributeProperty name="NotNullMethod" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="notNullMethod" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
      <itemType>
        <configurationElementMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/ValidationClass" />
      </itemType>
    </configurationElementCollection>
    <configurationElementCollection name="MappingAssemblies" xmlItemName="mappingAssembly" codeGenOptions="Indexer, AddMethod, RemoveMethod, GetItemMethods">
      <itemType>
        <configurationElementMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/MappingAssembly" />
      </itemType>
    </configurationElementCollection>
    <configurationElement name="ValidationClass">
      <attributeProperties>
        <attributeProperty name="StaticClass" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="staticClass" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="MappingAssembly">
      <attributeProperties>
        <attributeProperty name="AssemblyName" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="assemblyName" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElementCollection name="MappingNamespaces" xmlItemName="mappingNamespace" codeGenOptions="Indexer, AddMethod, RemoveMethod, GetItemMethods">
      <itemType>
        <configurationElementMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/MappingNamespace" />
      </itemType>
    </configurationElementCollection>
    <configurationElement name="MappingNamespace">
      <attributeProperties>
        <attributeProperty name="Namespace" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="namespace" isReadOnly="false">
          <type>
            <externalTypeMoniker name="/4744f52c-8c2b-4598-b110-6c5024d479d7/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
  </configurationElements>
  <propertyValidators>
    <validators />
  </propertyValidators>
</configurationSectionModel>