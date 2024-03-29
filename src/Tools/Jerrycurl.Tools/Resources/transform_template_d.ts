﻿declare interface SchemaModel {
    tables: TableModel[]
    imports: string[]
    types: TypeModel[]
    flags: { [key: string]: string }
}

declare interface TableModel {
    schema: string
    name: string
    ignore: boolean
    clr: ClassModel
    columns: ColumnModel[]
}

declare interface ClassModel {
    namespace: string
    name: string
    modifiers: string[]
    baseTypes: string[]
    isStruct: boolean
}

declare interface ColumnModel {
    name: string
    typeName: string
    isNullable: boolean
    isIdentity: boolean
    ignore: boolean
    keys: KeyModel[]
    references: ReferenceModel[]
    clr: PropertyModel
}

declare interface PropertyModel {
    modifiers: string[]
    typeName: string
    name: string
    isJson: boolean
    isInput: boolean
    isOutput: boolean
}

declare interface KeyModel {
    name: string
    index: number
}

declare interface ReferenceModel {
    name: string
    keyName: string
    keyIndex: number
}

declare interface TypeModel {
    dbName: string
    clrName: string
    isNullable: boolean
}