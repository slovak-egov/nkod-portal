import GridColumn from '../../components/GridColumn';
import GridRow from '../../components/GridRow';

type ColumnContent = React.ReactNode | string;

type Props = {
    columns: ColumnContent[];
};

export default function GridArrangement(props: Props) {
    const { columns } = props;

    const size = 4;

    const chunks = [];
    for (let i = 0; i < columns.length; i += size) {
        chunks.push(columns.slice(i, i + size));
    }

    if (chunks.length === 0) {
        return null;
    }

    return (
        <>
            {chunks.map((row, index) => (
                <GridRow key={index}>
                    {row.map((column, index) => (
                        <GridColumn key={index} widthUnits={1} totalUnits={4}>
                            {column}
                        </GridColumn>
                    ))}
                </GridRow>
            ))}
        </>
    );
}
