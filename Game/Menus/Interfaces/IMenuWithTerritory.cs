using Game.Territories;

namespace Game.Menus
{
    /// <summary>
    /// ���������, ����������� ���� ��� ���� � �����������.
    /// </summary>
    public interface IMenuWithTerritory : IMenu
    {
        public TableTerritory Territory { get; }
    }
}